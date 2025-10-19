namespace PFPackageManager
{
    /// <summary>
    /// 包来源
    /// </summary>
    public enum PackageSource
    {
        Unknown,
        BuiltIn,          // Unity内置包
        PackageManager,    // 通过PackageManager安装
        Git,              // Git仓库
        Local             // 本地包
    }

    /// <summary>
    /// 依赖状态信息
    /// </summary>
    public class DependencyStatus
    {
        public string packageName;
        public string requiredVersion;
        public bool isAvailable;
        public bool isUnityPackage;
        public string installedVersion;
        public bool isVersionCompatible;
        public bool canInstall;
        public PackageSource source;

        public string StatusText
        {
            get
            {
                if (!isAvailable)
                {
                    if (isUnityPackage)
                        return "Unity官方包 - 未安装";
                    else
                        return "第三方包 - 未安装";
                }
                else if (!isVersionCompatible)
                {
                    return $"已安装 ({installedVersion}) - 版本不匹配";
                }
                else
                {
                    // 根据来源显示不同的状态
                    switch (source)
                    {
                        case PackageSource.BuiltIn:
                            return $"Unity内置 ({installedVersion})";
                        case PackageSource.PackageManager:
                            return $"已通过PackageManager安装 ({installedVersion})";
                        default:
                            return $"已安装 ({installedVersion})";
                    }
                }
            }
        }
    }
}