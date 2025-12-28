using System.Collections.Generic;

namespace PFPackage
{
    public class PackageJsonParser
    {
        /// <summary>
        /// 解析 /-/all API 返回的所有包
        /// </summary>
        public static List<PackageInfo> ParseAllPackages(string json)
        {
            var packages = new List<PackageInfo>();
            if (string.IsNullOrEmpty(json)) return packages;
            var root = JSON.Parse(json);
            // /-/all 返回格式: { "_updated": xxx, "packageName": {...}, ... }
            foreach (var key in root.Keys) 
            {
                // 跳过 _updated 字段
                if (key.StartsWith("_"))
                    continue;

                var pkg = root[key];

                packages.Add(new PackageInfo
                {
                    PackageName = pkg["name"],
                    displayName = pkg["name"],
                    description = pkg["description"],
                    author = pkg["author"] != null ? pkg["author"]["name"] : "Unknown",
                    authorUrl = pkg["author"] != null ? pkg["author"]["url"] : "",
                    newestVersion = pkg["dist-tags"]["latest"],
                    isInstalled = false,
                    hasUpdate = false
                });
            }
            return packages;
        }
        
        /// <summary>
        /// 解析包详情（包含所有版本信息）
        /// </summary>
        public static PackageInfo ParsePackageDetail(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var root = JSON.Parse(json);
            var latestVersion = root["dist-tags"]["latest"].Value;

            var package = new PackageInfo
            {
                PackageName = root["name"],
                displayName = root["name"],  
                description = root["description"],
                // author =  root["author"] != null ? root["author"]["name"] : "Unknown",
                // authorUrl = root["author"] != null ? root["author"]["url"] : "",
                newestVersion = latestVersion,
                versions = new List<VersionInfo>()
            };

            // 解析所有版本（同时从最新版本获取 displayName 和 dependencies）
            var versionsNode = root["versions"];
            if (versionsNode != null)
            {
                foreach (var versionKey in versionsNode.Keys)
                {
                    var versionData = versionsNode[versionKey];
                    var timeNode = root["time"][versionKey];

                    // 从最新版本提取 displayName 和 dependencies
                    if (versionKey == latestVersion)
                    {
                        if (versionData["displayName"] != null)
                        {
                            package.displayName = versionData["displayName"];
                        }

                        // 解析依赖
                        var depsNode = versionData["dependencies"];
                        if (depsNode != null)
                        {
                            foreach (var depKey in depsNode.Keys)
                            {
                                package.dependencies[depKey] = depsNode[depKey];
                            }
                        }
                    }

                    package.versions.Add(new VersionInfo
                    {
                        version = versionKey,
                        publishDate = timeNode != null ? timeNode.Value : "Unknown",
                    });
                }
            }

            // 按版本号降序排序
            package.versions.Sort((a, b) => VersionComparer.CompareVersion(b.version, a.version));

            return package;
        }
    }
}