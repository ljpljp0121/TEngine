using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace PFPackage.FeiShuExcel
{
    public partial class FeiShuUtils
    {
        /// <summary>
        /// 创建飞书表格
        /// </summary>
        private static async Task<string> CreateFeiShuExcel(string excelName, string parentFolderToken)
        {
            string excelToken = "";
            try
            {
                using var httpClient = new HttpClient();

                string token = await GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    title = excelName,
                    folder_token = parentFolderToken
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(URL_CREATE_EXCEL, content);

                var responseString = await response.Content.ReadAsStringAsync();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(responseString);

                excelToken = jObject?["data"]?["spreadsheet"]?["spreadsheet_token"]?.ToString();
                Debug.Log($"[飞书读表] 创建表格成功，表格Token: {excelToken}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 创建飞书表格时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }
            return excelToken;
        }

        /// <summary>
        /// 获取飞书表格信息
        /// </summary>
        private static async Task<FeiShuExcelInfo> GetSheetInfo(string excelToken)
        {
            if (FeiShuExcelSetting.I.ExcelInfoDic.TryGetValue(excelToken, out var excelInfo))
            {
                return excelInfo;
            }
            excelInfo = new FeiShuExcelInfo();
            excelInfo.ExcelToken = excelToken;
            try
            {
                using var httpClient = new HttpClient();

                string token = await GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestUrl = string.Format(URL_GET_SHEET_INFO, excelToken);
                var response = await httpClient.GetAsync(requestUrl);
                var responseString = await response.Content.ReadAsStringAsync();

                JObject jObject = JsonConvert.DeserializeObject<JObject>(responseString);

                if (!(jObject?["data"]?["sheets"] is JArray sheets)) return excelInfo;

                foreach (var sheet in sheets)
                {
                    string sheetTitle = sheet["title"]?.ToString();
                    string sheetId = sheet["sheet_id"]?.ToString();
                    int index = sheet["index"]?.ToObject<int>() ?? 0;

                    if (string.IsNullOrEmpty(sheetId)) continue;
                    excelInfo.SheetInfos.Add(new FeiShuSheetInfo
                    {
                        SheetTitle = sheetTitle,
                        SheetId = sheetId,
                        Index = index
                    });
                    FeiShuExcelSetting.I.ExcelInfoDic[excelToken] = excelInfo;
                }
                EditorUtility.SetDirty(FeiShuExcelSetting.I);
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 获取飞书表格信息时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }
            return excelInfo;
        }

        /// <summary>
        /// 创建工作表
        /// </summary>
        private static async Task<FeiShuSheetInfo> CreateSheet(string excelToken, string sheetTitle, int index)
        {
            try
            {
                using var httpClient = new HttpClient();

                string token = await GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    requests = new[]
                    {
                        new
                        {
                            addSheet = new
                            {
                                properties = new
                                {
                                    title = sheetTitle,
                                    index = index,
                                }
                            }
                        }
                    }
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(string.Format(URL_UPDATE_SHEET, excelToken), content);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[飞书读表] HTTP请求失败: {response.StatusCode}");
                    return null;
                }
                var responseString = await response.Content.ReadAsStringAsync();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(responseString);

                string sheetId = jObject?["replies"]?["addSheet"]?["properties"]?["sheetId"]?.ToString();
                Debug.Log($"[飞书读表] 创建工作表 [{index}]: {sheetTitle}");
                return new FeiShuSheetInfo
                {
                    SheetTitle = sheetTitle,
                    SheetId = sheetId,
                    Index = index
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 飞书表格创建工作表时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }
            return null;
        }

        /// <summary>
        /// 删除工作表
        /// </summary>
        private static async Task DeleteSheet(string excelToken, string sheetId)
        {
            try
            {
                using var httpClient = new HttpClient();

                string token = await GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    requests = new[]
                    {
                        new
                        {
                            deleteSheet = new
                            {
                                sheetId = sheetId,
                            }
                        }
                    }
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(string.Format(URL_UPDATE_SHEET, excelToken), content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 飞书表格删除工作表时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 更新工作表信息
        /// </summary>
        private static async Task UpdateSheet(string excelToken, string sheetId, string newTitle, int index)
        {
            try
            {
                using var httpClient = new HttpClient();

                string token = await GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    requests = new[]
                    {
                        new
                        {
                            updateSheet = new
                            {
                                properties = new
                                {
                                    sheetId = sheetId,
                                    title = newTitle,
                                    index = index,
                                },
                            }
                        }
                    }
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(string.Format(URL_UPDATE_SHEET, excelToken), content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 飞书表格删除工作表时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }
        }

        private static async Task WriteOnlineSheet(string excelToken, string sheetId, string[,] sheetData)
        {
            try
            {
                using var httpClient = new HttpClient();
                string token = await GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var requestBody = JsonConvert.SerializeObject(new
                {
                    valueRange = new
                    {
                        range = sheetId,
                        values = sheetData
                    }
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                await httpClient.PutAsync(string.Format(URL_WRITE_SHEET, excelToken), content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 写入飞书表格时发生错误: {ex.Message}");
            }
        }

        /// <summary>   
        /// 将本地Excel文件内容同步到飞书云端
        /// </summary>
        /// <param name="excelToken"></param>
        /// <param name="localFilePath"></param>
        public static async Task WriteOnlineExcel(string excelToken, string localFilePath)
        {
            try
            {
                FeiShuExcelInfo onlineInfo = await GetSheetInfo(excelToken);
                LocalExcelInfo localInfo = ReadExcelFile(localFilePath);
                if (localInfo == null) return;

                int localCount = localInfo.SheetInfos.Count;
                int onlineCount = onlineInfo.SheetInfos.Count;

                Debug.Log($"[飞书同步] 本地工作表: {localCount}个, 云端工作表:{onlineCount}个");

                //数量对齐
                if (localCount > onlineCount)
                {
                    int toCreate = localCount - onlineCount;
                    for (int i = 0; i < toCreate; i++)
                    {
                        var newSheet = await CreateSheet(excelToken, localInfo.SheetInfos[localCount - 1 - i].SheetTitle, i);
                        onlineInfo.SheetInfos.Add(newSheet);
                    }
                    Debug.Log($"[飞书同步] 创建 {toCreate} 个工作表");
                }
                else
                {
                    int toDelete = onlineCount - localCount;
                    for (int i = 0; i < toDelete; i++)
                    {
                        await DeleteSheet(excelToken, onlineInfo.SheetInfos[onlineCount - 1 - i].SheetId);
                        onlineInfo.SheetInfos.RemoveAt(onlineCount - 1 - i);
                    }
                    Debug.Log($"[飞书同步] 删除 {toDelete} 个工作表");
                }

                for (int i = 0; i < localInfo.SheetInfos.Count; i++)
                {
                    var localSheet = localInfo.SheetInfos[i];
                    var onlineSheet = onlineInfo.SheetInfos[i];
                    await UpdateSheet(excelToken, onlineSheet.SheetId, localSheet.SheetTitle, i);
                    await WriteOnlineSheet(excelToken, onlineSheet.SheetId, localSheet.SheetData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 写入飞书表格时发生错误: {ex.Message}");
            }
        }
    }
}