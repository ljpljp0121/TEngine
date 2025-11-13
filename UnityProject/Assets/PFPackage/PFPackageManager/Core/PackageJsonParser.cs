using System;
using System.Collections.Generic;
using UnityEngine;
using TEngine.Localization.SimpleJSON;

namespace PFPackageManager
{
    /// <summary>
    /// 专门负责解析包的JSON数据
    /// </summary>
    public static class PackageJsonParser
    {
        /// <summary>
        /// 解析 /-/all API 返回的所有包
        /// </summary>
        public static List<PackageInfo> ParseAllPackages(string json)
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

                packages.Add(new PackageInfo
                {
                    name = pkg["name"],
                    displayName = pkg["name"],
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
        /// 解析包详情（包含所有版本信息）
        /// </summary>
        public static PackageInfo ParsePackageDetail(string json)
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
                        changelog = "" // TODO: 从哪里获取 changelog？
                    });
                }
            }

            // 按版本号降序排序
            package.versions.Sort((a, b) => VersionComparer.CompareVersion(b.version, a.version));

            return package;
        }
    }
}