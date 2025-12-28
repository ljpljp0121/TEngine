using System;
using System.Collections.Generic;

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
        
        public bool isInstalled;
        public bool hasUpdate;
    }
}