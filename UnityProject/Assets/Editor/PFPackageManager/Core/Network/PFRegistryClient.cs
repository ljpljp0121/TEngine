using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PFPackageManager
{
    /// <summary>
    /// NPM Registry API 客户端 - 只负责网络请求
    /// </summary>
    public class PFRegistryClient
    {
        private readonly string registryUrl;

        public PFRegistryClient(string url)
        {
            registryUrl = url.TrimEnd('/');
        }

        /// <summary>
        /// 获取所有包（使用 Verdaccio /-/all API）
        /// </summary>
        public void GetAllPackages(Action<List<PackageInfo>> onSuccess, Action<string> onError)
        {
            // Verdaccio API: /-/all 返回所有包
            string url = $"{registryUrl}/-/all";
            FetchJson(url, (json) =>
            {
                try
                {
                    var packages = PackageJsonParser.ParseAllPackages(json);
                    onSuccess?.Invoke(packages);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"解析包列表失败: {e.Message}\n{e.StackTrace}");
                }
            }, onError);
        }

        /// <summary>
        /// 获取指定包的详细信息（包含所有版本）
        /// </summary>
        public void GetPackageDetail(string packageName, Action<PackageInfo> onSuccess, Action<string> onError)
        {
            // NPM Registry API: /{packageName}
            string url = $"{registryUrl}/{packageName}";
            FetchJson(url, (json) =>
            {
                try
                {
                    var package = PackageJsonParser.ParsePackageDetail(json);
                    onSuccess?.Invoke(package);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"解析包详情失败: {e.Message}\n{e.StackTrace}");
                }
            }, onError);
        }

        /// <summary>
        /// 通用 HTTP GET 请求（异步）
        /// </summary>
        private void FetchJson(string url, Action<string> onSuccess, Action<string> onError)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);

            // 设置超时时间（10秒）
            request.timeout = 10;

            var operation = request.SendWebRequest();

            operation.completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                }
                else
                {
                    string errorMsg;
                    switch (request.result)
                    {
                        case UnityWebRequest.Result.ConnectionError:
                            errorMsg = $"网络连接错误: {request.error}\nURL: {url}\n请检查网络连接或防火墙设置";
                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            errorMsg = $"HTTP协议错误: {request.error}\n状态码: {request.responseCode}\nURL: {url}";
                            break;
                        case UnityWebRequest.Result.DataProcessingError:
                            errorMsg = $"数据处理错误: {request.error}\nURL: {url}";
                            break;
                        default:
                            errorMsg = $"请求失败: {request.error}\nURL: {url}\n错误代码: {request.result}";
                            break;
                    }
                    onError?.Invoke(errorMsg);
                }
                request.Dispose();
            };
        }

      }
}
