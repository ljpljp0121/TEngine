using System;

namespace PFPackage
{
    /// <summary>
    /// 版本号比较工具（基于 SemanticVersion 2.0.0 规范）
    /// </summary>
    public static class VersionComparer
    {
        /// <summary>
        /// 比较版本号（语义化版本）
        /// </summary>
        /// <returns>大于0表示v1>v2，小于0表示v1<v2，等于0表示相等</returns>
        public static int CompareVersion(string v1, string v2)
        {
            // 处理空值或空字符串
            if (string.IsNullOrEmpty(v1) && string.IsNullOrEmpty(v2))
                return 0;
            if (string.IsNullOrEmpty(v1))
                return -1;  // 空版本被认为更小
            if (string.IsNullOrEmpty(v2))
                return 1;   // 空版本被认为更小

            // 尝试解析版本，如果失败则作为特殊版本处理
            try
            {
                var version1 = SemanticVersion.Parse(v1);
                var version2 = SemanticVersion.Parse(v2);
                return version1.CompareTo(version2);
            }
            catch (Exception)
            {
                // 如果解析失败，按字符串比较
                return string.Compare(v1, v2, StringComparison.Ordinal);
            }
        }
    }
}