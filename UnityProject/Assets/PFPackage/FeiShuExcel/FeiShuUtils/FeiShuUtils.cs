using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using OfficeOpenXml;


namespace PFPackage.FeiShuExcel
{
    public static class FeiShuUtils
    {
        public const string URL_GET_FOLDER_CHECK_LIST = "https://open.feishu.cn/open-apis/drive/v1/files";
        public const string URL_CREATE_FOLDER = "https://open.feishu.cn/open-apis/drive/v1/files/create_folder";
        public const string URL_CREATE_EXCEL = "https://open.feishu.cn/open-apis/sheets/v3/spreadsheets";
        public const string URL_WRITE_SHEET = "https://open.feishu.cn/open-apis/sheets/v2/spreadsheets/{0}/values";
        public const string URL_GET_SHEET_INFO = "https://open.feishu.cn/open-apis/sheets/v3/spreadsheets/{0}/sheets/query";
        
        /// <summary>
        /// 获取文件夹 下的文件清单(不可递归)
        /// </summary>
        private static async Task<List<FeiShuFileInfo>> GetFolderRootCheckList(string folderToken)
        {
            var result = new List<FeiShuFileInfo>();

            try
            {
                using var httpClient = new HttpClient();

                string token = await FeiShuAuthUtils.GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestUrl = $"{URL_GET_FOLDER_CHECK_LIST}?folder_token={folderToken}";
                var response = await httpClient.GetAsync(requestUrl);
                var responseString = await response.Content.ReadAsStringAsync();

                JObject jObject = JsonConvert.DeserializeObject<JObject>(responseString);

                // 解析文件列表
                if (!(jObject?["data"]?["files"] is JArray files)) return result;

                foreach (var file in files)
                {
                    string name = file["name"]?.ToString() ?? "";
                    string fileToken = file["token"]?.ToString() ?? "";
                    string fileType = file["type"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(fileToken)) continue;

                    result.Add(new FeiShuFileInfo()
                    {
                        Name = name,
                        Token = fileToken,
                        Type = fileType
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 获取飞书目标文件夹Token时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }

            return result;
        }

        /// <summary>
        /// 创建飞书文件夹
        /// </summary>
        private static async Task<string> CreateFeiShuFolder(string folderName, string parentFolderToken)
        {
            string folderToken = "";
            try
            {
                using var httpClient = new HttpClient();

                string token = await FeiShuAuthUtils.GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestBody = JsonConvert.SerializeObject(new
                {
                    name = folderName,
                    folder_token = parentFolderToken
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(URL_CREATE_FOLDER, content);

                var responseString = await response.Content.ReadAsStringAsync();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(responseString);

                folderToken = jObject?["data"]?["token"]?.ToString();
                Debug.Log($"[飞书读表] 创建文件夹成功 文件夹Token:{folderToken}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 创建飞书文件夹时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }
            return folderToken;
        }

        /// <summary>
        /// 创建飞书表格
        /// </summary>
        private static async Task<string> CreateFeiShuExcel(string excelName, string parentFolderToken)
        {
            string excelToken = "";
            try
            {
                using var httpClient = new HttpClient();

                string token = await FeiShuAuthUtils.GetOnlineToken();
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
        /// 读取本地Excel文件内容
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <returns></returns>
        private static List<List<string>> ReadExcelFile(string localFilePath)
        {
            var data = new List<List<string>>();

            try
            {
                var fileInfo = new FileInfo(localFilePath);
                if (!fileInfo.Exists)
                {
                    Debug.LogError($"[飞书读表] Excel文件不存在: {localFilePath}");
                    return null;
                }

                using var package = new ExcelPackage(fileInfo);
                // 获取第一个工作表
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                // 读取所有有数据的单元格
                var dimension = worksheet.Dimension;
                if (dimension == null)
                {
                    Debug.LogWarning($"[飞书读表] 工作表为空: {worksheet.Name}");
                    return data;
                }

                // 遍历每一行
                for (int row = dimension.Start.Row; row <= dimension.End.Row; row++)
                {
                    var rowData = new List<string>();

                    for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Text;
                        rowData.Add(cellValue);
                    }
                    if (rowData.Any(value => !string.IsNullOrEmpty(value)))
                        data.Add(rowData);
                }
                Debug.Log($"[飞书读表] 读取工作表完成: {data.Count} 行数据");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 读取Excel文件失败: {localFilePath}\n错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取飞书表格信息
        /// </summary>
        public static async Task<FeiShuExcelInfo> GetSheetInfo(string excelToken)
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

                string token = await FeiShuAuthUtils.GetOnlineToken();
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
        /// 将本地Excel文件内容同步到飞书云端
        /// </summary>
        /// <param name="excelToken"></param>
        /// <param name="localFilePath"></param>
        public static async Task WriteOnlineExcel(string excelToken, string localFilePath)
        {
            try
            {
                using var httpClient = new HttpClient();
                string token = await FeiShuAuthUtils.GetOnlineToken();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                List<List<string>> excelData = ReadExcelFile(localFilePath);

                var requestBody = JsonConvert.SerializeObject(new
                {
                    valueRange = new
                    {
                        range = "6eb039",
                        values = excelData
                    }
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(string.Format(URL_WRITE_SHEET, excelToken), content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[飞书读表] 成功写入表格数据: {excelToken}");
                }
                else
                {
                    Debug.LogError($"[飞书读表] 写入表格数据失败: {response.StatusCode}, {responseString}");
                }
            }

            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 写入飞书表格时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否为 Excel 文件
        /// </summary>
        private static bool IsExcelFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLower();
            return extension == ".xlsx" || extension == ".xls";
        }

        /// <summary>
        /// 递归同步目录结构到飞书云端
        /// </summary>
        /// <param name="localPath">本地目录路径</param>
        /// <param name="parentToken">云端父文件夹 token</param>
        /// <returns>本地路径到云端 token 的映射字典</returns>
        public static async Task<Dictionary<string, string>> SyncDirectoryStructure(string localPath, string parentToken)
        {
            var pathToTokenMap = new Dictionary<string, string>();

            try
            {
                // 获取云端当前目录的文件列表
                var existingFiles = await GetFolderRootCheckList(parentToken);

                // 云端的所有子文件夹和表格
                var folders = existingFiles.Where(f => f.Type == "folder").ToDictionary(f => f.Name, f => f.Token);
                var excels = existingFiles.Where(f => f.Type == "sheet").ToDictionary(f => f.Name, f => f.Token);

                // 遍历本地目录
                foreach (var item in Directory.GetFileSystemEntries(localPath))
                {
                    var name = Path.GetFileNameWithoutExtension(item);

                    if (name.StartsWith("~")) continue;

                    //处理文件夹
                    if (Directory.Exists(item))
                    {
                        if (folders.TryGetValue(name, out string existingFolderToken))
                        {
                            pathToTokenMap[item] = existingFolderToken;
                        }
                        else
                        {
                            string newFolderToken = await CreateFeiShuFolder(name, parentToken);
                            pathToTokenMap[item] = newFolderToken;
                        }
                        var childMap = await SyncDirectoryStructure(item, pathToTokenMap[item]);
                        foreach (var kvp in childMap)
                        {
                            pathToTokenMap[kvp.Key] = kvp.Value;
                        }
                    }
                    //处理Excel文件
                    else if (File.Exists(item) && IsExcelFile(item))
                    {
                        if (excels.TryGetValue(name, out string existingSheetToken))
                        {
                            pathToTokenMap[item] = existingSheetToken;
                        }
                        else
                        {
                            string newExcelToken = await CreateFeiShuExcel(name, parentToken);
                            pathToTokenMap[item] = newExcelToken;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 同步目录时发生错误: {ex.Message}");
            }

            return pathToTokenMap;
        }
        
        /// <summary>
        /// 同步本地配置表到远端(递归)
        /// </summary>
        public static async Task SyncLocalExcelToOnline()
        {
            try
            {
                string localRootPath = FeiShuExcelSetting.I.LocalRootPath;

                if (string.IsNullOrEmpty(localRootPath))
                {
                    Debug.LogError("[飞书同步] 本地根目录路径未配置");
                    return;
                }

                if (!Directory.Exists(localRootPath))
                {
                    Debug.LogError($"[飞书同步] 本地根目录不存在: {localRootPath}");
                    return;
                }

                var syncResult = await SyncDirectoryStructure(localRootPath, FeiShuExcelSetting.I.FeiShuFolderRootToken);
                
                Debug.Log($"[飞书同步] 同步完成！当前共有 {syncResult.Count} 个文件(包括文件夹)" +
                          $" 映射信息：\n{string.Join("\n", syncResult.Select(kvp => $"{kvp.Key} -> {kvp.Value}"))}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书同步] 同步过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}