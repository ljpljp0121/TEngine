using System;

namespace PFPackage
{
    [Serializable]
    public class VersionInfo
    {
        public string version;
        public string publishDate;
        public string changelog;
        public bool isInstalled;
    }
}