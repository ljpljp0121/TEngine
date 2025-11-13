using System;
using System.Collections.Generic;

namespace PFPackageManager
{
    [Serializable]
    public class PackageInfo
    {
        // 基础信息
        public string name;
        public string displayName;
        public string description;
        public string author;
        public string authorUrl;

        // 版本信息
        public string version;           // 当前最新版本
        public string localVersion;      // 本地安装版本
        public List<VersionInfo> versions = new List<VersionInfo>();

        // 链接
        public string documentationUrl;
        public string changelogUrl;
        public string licensesUrl;

        // 依赖
        public Dictionary<string, string> dependencies = new Dictionary<string, string>();

        // 状态
        public bool isInstalled;
        public bool hasUpdate;

        // 元数据
        public string publishDate;
        public List<string> keywords = new List<string>();
    }

    [Serializable]
    public class VersionInfo
    {
        public string version;
        public string publishDate;
        public string changelog;
        public bool isInstalled;
    }
}
