namespace PFPackage
{
    /// <summary>
    /// 包来源
    /// </summary>
    public enum PackageSource
    {
        Unknown, // 未知
        UnityPackage, // Unity包
        PFPackage, // PFPackage
        Git, // Git下载
    }

    public class DependencyStatus
    {
        // 状态字段（所有包通用）
        public string packageName; // 包名
        public string requiredVersion; // 需求版本
        public string installedVersion; // 已安装版本
        public bool isInstalled; // 是否已安装
        public bool isCompatible; // 版本是否兼容
        public PackageSource source; // 来源

        // 跳转功能（仅 PFPackage 使用）
        public PackageInfo relatedPackage; // 关联的 PackageInfo（仅用于跳转）

        // 版本解析字段
        public string resolvedVersion; // 解析后的目标版本（用于安装/更新）

        /// <summary>
        /// 获取状态显示的文本，格式：来源:PFPackage(已安装 1.0.0)
        /// </summary>
        public string GetStatusText()
        {
            string sourceName = GetSourceName();
            string statusText = GetStatusDetail();

            return $"来源:{sourceName}({statusText})";
        }

        private string GetSourceName()
        {
            switch (source)
            {
                case PackageSource.UnityPackage:
                    return "Unity包";
                case PackageSource.PFPackage:
                    return "PFPackage";
                case PackageSource.Git:
                    return "Git";
                default:
                    return "未知";
            }
        }

        private string GetStatusDetail()
        {
            if (!isInstalled)
                return "未安装";

            if (!isCompatible)
                return $"版本不兼容 已安装:{installedVersion} 需要:{requiredVersion}";

            return $"已安装 {installedVersion}";
        }

        /// <summary>
        /// 获取状态颜色
        /// </summary>
        public UnityEngine.Color GetStatusColor()
        {
            if (!isInstalled)
                return UnityEngine.Color.yellow;

            if (!isCompatible)
                return UnityEngine.Color.red;

            return UnityEngine.Color.green;
        }
    }
}