using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PFPackage
{
    [Serializable]
    public class PackageInfo
    {
        public string PackageName; //包名
        public string displayName; //显示名
        public string description; //描述
        public string author; //作者
        public string authorUrl; //作者链接

        public string newestVersion; // 当前最新版本
        public string localVersion; // 本地安装版本
        public List<VersionInfo> versions = new List<VersionInfo>(); //所有版本信息

        public string documentationUrl; //文档链接
        public string changelogUrl; //更新日志链接

        public Dictionary<string, string> dependencies = new Dictionary<string, string>(); //依赖

        public bool IsInstalled;
        public bool HasUpdate;

        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(displayName))
                return PackageName;
            else
                return displayName;
        }

        public string GetDirectory()
        {
            string directoryName = GetDisplayName();
            return Path.Combine(PFPackageConfig.I.InstallPath, directoryName);
        }

        /// <summary>
        /// 包是否已下载
        /// </summary>
        public bool IsPackageInstalled()
        {
            return Directory.Exists(GetDirectory());
        }

        /// <summary>
        /// 获取下载包的版本
        /// </summary>
        private string GetInstalledVersion()
        {
            return FileSystemOperations.ReadPackageVersion(GetDirectory());
        }

        /// <summary>
        /// 比较版本号
        /// </summary>
        private int CompareVersions(string v1, string v2)
        {
            return VersionComparer.CompareVersion(v1, v2);
        }


        public void RefreshStatus()
        {
            IsInstalled = IsPackageInstalled();
            if (IsInstalled)
            {
                localVersion = GetInstalledVersion();
                HasUpdate = CompareVersions(newestVersion, localVersion) > 0;
                RefreshVersion();
            }
            else //没有下载版本
            {
                localVersion = null;
                HasUpdate = false;
                foreach (var version in versions)
                {
                    version.isInstalled = false;
                }
            }
        }

        private void RefreshVersion()
        {
            foreach (var version in versions)
            {
                int value = CompareVersions(version.version, localVersion);
                version.isInstalled = value == 0;
            }
        }

        /// <summary>
        /// 解析版本范围，从依赖包的所有版本中选择最佳版本
        /// </summary>
        public string ResolveVersion(string versionRange)
        {
            // 如果没有指定范围，返回最新版本
            if (string.IsNullOrEmpty(versionRange) || versionRange == "*")
                return newestVersion;

            var availableVersions = new List<string>() { newestVersion };
            if (versions != null)
            {
                availableVersions.AddRange(versions.Select(v => v.version));
            }
            
            var versionRangeParser = new VersionRange(versionRange);
            string bestVersion = versionRangeParser.SelectBestVersion(availableVersions);
            
            if (string.IsNullOrEmpty(bestVersion))
            {
                PFLog.LogWarning($"无法找到满足 {versionRange} 的版本，将使用最新版本 {newestVersion}");
                return newestVersion;
            }
            
            PFLog.Log($"版本范围 '{versionRange}' 解析为: {bestVersion}");
            return bestVersion;
        }
    }
}