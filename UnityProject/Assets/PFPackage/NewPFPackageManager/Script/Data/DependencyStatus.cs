namespace PFPackage
{
    /// <summary>
    /// 包来源
    /// </summary>
    public enum PackageSource
    {
        Unknown,        // 未知
        UnityPackage,   // Unity包
        PFPackage,      // PFPackage
        Git,            // Git下载
    }
    
    public class DependencyStatus
    {
        public string packageName;
        public string requiredVersion;
        public string installedVersion;
        public PackageSource source;
    }
}