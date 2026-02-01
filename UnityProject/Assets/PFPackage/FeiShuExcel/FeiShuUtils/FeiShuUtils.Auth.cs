using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace PFPackage.FeiShuExcel
{
    public static partial class FeiShuUtils
    {
        /// <summary>
        /// 获取 飞书 tenant_access_token 用于鉴权
        /// </summary>
        private static async Task GetAccessToken()
        {
            try
            {
                using var httpClient = new HttpClient();
                var requestBody = JsonConvert.SerializeObject(new
                {
                    app_id = FeiShuExcelSetting.I.APPID,
                    app_secret = FeiShuExcelSetting.I.APPSecret
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(URL_GET_ACCESS_TOKEN, content);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[飞书读表] HTTP请求失败: {response.StatusCode}");
                    return;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(responseString);

                if (jObject?["tenant_access_token"] == null)
                {
                    Debug.LogError("[飞书读表] 飞书返回的Token格式错误");
                    return;
                }

                string token = jObject["tenant_access_token"].ToString();
                long expireTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)jObject["expire"];

                EditorPrefs.SetString("FeiShu_Token", token);
                EditorPrefs.SetString("FeiShu_Expire", expireTime.ToString());

                Debug.Log("[飞书读表] 飞书Token获取成功");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 获取飞书tenant_access_token时发生错误: {ex.Message} 堆栈信息:{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取飞书 tenant_access_token 通过过期时间判断是否刷新
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetOnlineToken()
        {
            string token = EditorPrefs.GetString("FeiShu_Token");
            long endTime = long.Parse(EditorPrefs.GetString("FeiShu_Expire", "0"));
            if (endTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                await GetAccessToken();
                token = EditorPrefs.GetString("FeiShu_Token");
            }
            return token;
        }
    }
}