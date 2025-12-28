using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PFPackage
{
    public class PackageLoader
    {
        /// <summary>
        /// 获取所有包
        /// </summary>
        /// <returns></returns>
        public async Task<List<PackageInfo>> GetAllPackages()
        {
            string url = $"{PFPackageConfig.I.RegistryUrl}/-/all";
            string json = await FetchJsonAsync(url);
            Debug.Log($"[PFPackageManager] 获取所有包信息: {json}");
            return PackageJsonParser.ParseAllPackages(json);
        }

        /// <summary>
        /// 获取指定包的详细信息
        /// </summary>
        public async Task<PackageInfo> GetPackageDetailAsync(string packageName)
        {
            string url = $"{PFPackageConfig.I.RegistryUrl}/{packageName}";
            string json = await FetchJsonAsync(url);
            Debug.Log($"[PFPackageManager] 获取包 {packageName} 详细信息: {json}");
            return PackageJsonParser.ParsePackageDetail(json);
        }
        
        /// <summary>
        /// 通用HTTP GET请求
        /// </summary>
        private async Task<string> FetchJsonAsync(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            string auth = System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{PFPackageConfig.I.UserName}:{PFPackageConfig.I.Password}")
            );
            request.SetRequestHeader("Authorization", $"Basic {auth}");
            request.timeout = 10;

            var operation = request.SendWebRequest();
            
            // 等待请求完成或取消
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"网络请求失败: {request.error}");
                return null;
            }

            return request.downloadHandler.text;
        }
    }
}