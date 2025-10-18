using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TEngine.Localization.SimpleJSON;

namespace PFPackageManager
{
    /// <summary>
    /// NPM Registry API 客户端
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
                    var packages = ParseAllPackages(json);
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
                    var package = ParsePackageDetail(json);
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
            var operation = request.SendWebRequest();

            operation.completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                }
                else
                {
                    onError?.Invoke($"请求失败: {request.error}");
                }
                request.Dispose();
            };
        }

        /// <summary>
        /// 解析 /-/all API 返回的所有包
        /// </summary>
        private List<PackageInfo> ParseAllPackages(string json)
        {
            var packages = new List<PackageInfo>();
            var root = JSON.Parse(json);

            // /-/all 返回格式: { "_updated": xxx, "packageName": {...}, ... }
            foreach (var key in root.Keys)
            {
                // 跳过 _updated 字段
                if (key.StartsWith("_"))
                    continue;

                var pkg = root[key];
                var latestVersion = pkg["dist-tags"]["latest"];

                // displayName 在 versions 对象里，需要从最新版本获取
                string displayName = pkg["name"];
                var versionsNode = pkg["versions"];
                if (versionsNode != null && latestVersion != null)
                {
                    var latestVersionData = versionsNode[latestVersion.Value];
                    if (latestVersionData != null && latestVersionData["displayName"] != null)
                    {
                        displayName = latestVersionData["displayName"];
                    }
                }

                packages.Add(new PackageInfo
                {
                    name = pkg["name"],
                    displayName = displayName,
                    description = pkg["description"],
                    author = pkg["author"] != null ? pkg["author"]["name"] : "Unknown",
                    authorUrl = pkg["author"] != null ? pkg["author"]["url"] : "",
                    version = latestVersion,
                    publishDate = pkg["time"]["modified"],
                    isInstalled = false,
                    hasUpdate = false
                });
            }

            return packages;
        }

        /// <summary>
        /// 解析包详情（使用 SimpleJSON，包含所有版本信息）
        /// </summary>
        private PackageInfo ParsePackageDetail(string json)
        {
            var root = JSON.Parse(json);
            var latestVersion = root["dist-tags"]["latest"].Value;

            var package = new PackageInfo
            {
                name = root["name"],
                displayName = root["name"],  // 默认值
                description = root["description"],
                version = latestVersion,
                versions = new List<VersionInfo>()
            };

            // 解析作者信息
            if (root["author"] != null)
            {
                package.author = root["author"]["name"];
                package.authorUrl = root["author"]["url"];
            }

            // 解析所有版本（同时从最新版本获取 displayName）
            var versionsNode = root["versions"];
            if (versionsNode != null)
            {
                foreach (var versionKey in versionsNode.Keys)
                {
                    var versionData = versionsNode[versionKey];
                    var timeNode = root["time"][versionKey];

                    // 从最新版本提取 displayName
                    if (versionKey == latestVersion && versionData["displayName"] != null)
                    {
                        package.displayName = versionData["displayName"];
                    }

                    package.versions.Add(new VersionInfo
                    {
                        version = versionKey,
                        publishDate = timeNode != null ? timeNode.Value : "Unknown",
                        changelog = "" // TODO: 从哪里获取 changelog？
                    });
                }
            }

            // 按版本号降序排序
            package.versions.Sort((a, b) => CompareVersion(b.version, a.version));

            return package;
        }


        private int CompareVersion(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                int n1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                int n2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                if (n1 != n2)
                    return n1.CompareTo(n2);
            }

            return 0;
        }
    }
}
