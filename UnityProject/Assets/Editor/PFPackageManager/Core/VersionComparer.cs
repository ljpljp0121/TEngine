using System;

namespace PFPackageManager
{
    /// <summary>
    /// 版本号比较工具
    /// </summary>
    public static class VersionComparer
    {
        /// <summary>
        /// 比较版本号（语义化版本）
        /// </summary>
        /// <returns>大于0表示v1>v2，小于0表示v1<v2，等于0表示相等</returns>
        public static int CompareVersion(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                int n1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                int n2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                if (n1 != n2)
                    return n1.CompareTo(n2);
            }

            return 0;
        }
    }
}