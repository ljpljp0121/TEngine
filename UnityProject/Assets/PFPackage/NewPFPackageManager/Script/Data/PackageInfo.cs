using System;
using System.Collections.Generic;
using System.IO;

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
    }
}