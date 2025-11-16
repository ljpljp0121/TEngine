using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PFPackage.FeiShuExcel
{
    public partial class FeiShuUtils
    {
        /// <summary>  
        /// 获取文件夹 下的文件清单(不可递归)
        /// </summary>
        private static async Task<List<FeiShuFileInfo>> GetFolderRootCheckList(string folderToken)
        {
            var result = new List<FeiShuFileInfo>();

            try
            {
                using var httpClient = new HttpClient();

                string token = await GetOnlineToken();
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

                string token = await GetOnlineToken();
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
    }
}