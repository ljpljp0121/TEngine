using System;
using System.Text.RegularExpressions;

namespace PFPackageManager
{
    /// <summary>
    /// 语义化版本对象（Semantic Versioning 2.0.0）
    /// 格式: Major.Minor.Patch-PreRelease+BuildMetadata
    /// </summary>
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Patch { get; private set; }
        public string PreRelease { get; private set; }  // alpha.1, beta.2, rc.1
        public string BuildMetadata { get; private set; }  // 20250120

        // 正则表达式（符合 SemVer 2.0.0 规范）
        private static readonly Regex VersionRegex = new Regex(
            @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)" +
            @"(?:-(?<prerelease>[0-9A-Za-z\-\.]+))?" +
            @"(?:\+(?<buildmetadata>[0-9A-Za-z\-\.]+))?$",
            RegexOptions.Compiled
        );

        /// <summary>
        /// 解析版本字符串
        /// </summary>
        public static SemanticVersion Parse(string version)
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("版本字符串不能为空");

            var match = VersionRegex.Match(version.Trim());
            if (!match.Success)
                throw new FormatException($"无效的版本格式: {version}");

            return new SemanticVersion
            {
                Major = int.Parse(match.Groups["major"].Value),
                Minor = int.Parse(match.Groups["minor"].Value),
                Patch = int.Parse(match.Groups["patch"].Value),
                PreRelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null,
                BuildMetadata = match.Groups["buildmetadata"].Success ? match.Groups["buildmetadata"].Value : null
            };
        }

        /// <summary>
        /// 尝试解析（不抛异常）
        /// </summary>
        public static bool TryParse(string version, out SemanticVersion result)
        {
            try
            {
                result = Parse(version);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// 比较两个版本（符合 SemVer 规范）
        /// </summary>
        public int CompareTo(SemanticVersion other)
        {
            if (other == null) return 1;

            // 1. 比较 Major.Minor.Patch
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            if (Patch != other.Patch) return Patch.CompareTo(other.Patch);

            // 2. 处理预发布版本
            // 规则: 1.0.0 > 1.0.0-alpha（正式版大于预发布版）
            if (string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
                return 1;
            if (!string.IsNullOrEmpty(PreRelease) && string.IsNullOrEmpty(other.PreRelease))
                return -1;

            // 3. 都是预发布版本，比较标识符
            if (!string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
            {
                return ComparePreRelease(PreRelease, other.PreRelease);
            }

            // 4. Build Metadata 不参与比较（SemVer 规范）
            return 0;
        }

        /// <summary>
        /// 比较预发布版本标识符
        /// 例如: alpha < alpha.1 < alpha.beta < beta < beta.2 < rc.1
        /// </summary>
        private int ComparePreRelease(string pre1, string pre2)
        {
            var parts1 = pre1.Split('.');
            var parts2 = pre2.Split('.');

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                // 如果一个已结束，则较短的更小
                if (i >= parts1.Length) return -1;
                if (i >= parts2.Length) return 1;

                string p1 = parts1[i];
                string p2 = parts2[i];

                // 尝试作为数字比较
                bool isNum1 = int.TryParse(p1, out int num1);
                bool isNum2 = int.TryParse(p2, out int num2);

                if (isNum1 && isNum2)
                {
                    // 都是数字，按数字比较
                    if (num1 != num2) return num1.CompareTo(num2);
                }
                else if (isNum1)
                {
                    // 数字 < 字符串（SemVer 规范）
                    return -1;
                }
                else if (isNum2)
                {
                    // 字符串 > 数字
                    return 1;
                }
                else
                {
                    // 都是字符串，按字母顺序比较
                    int cmp = string.CompareOrdinal(p1, p2);
                    if (cmp != 0) return cmp;
                }
            }

            return 0;
        }

        public override string ToString()
        {
            string version = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(PreRelease))
                version += $"-{PreRelease}";
            if (!string.IsNullOrEmpty(BuildMetadata))
                version += $"+{BuildMetadata}";
            return version;
        }

        public override bool Equals(object obj)
        {
            return obj is SemanticVersion other && CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        // 运算符重载
        public static bool operator >(SemanticVersion v1, SemanticVersion v2) => v1.CompareTo(v2) > 0;
        public static bool operator <(SemanticVersion v1, SemanticVersion v2) => v1.CompareTo(v2) < 0;
        public static bool operator >=(SemanticVersion v1, SemanticVersion v2) => v1.CompareTo(v2) >= 0;
        public static bool operator <=(SemanticVersion v1, SemanticVersion v2) => v1.CompareTo(v2) <= 0;
        public static bool operator ==(SemanticVersion v1, SemanticVersion v2)
        {
            if (ReferenceEquals(v1, v2)) return true;
            if (v1 is null || v2 is null) return false;
            return v1.CompareTo(v2) == 0;
        }
        public static bool operator !=(SemanticVersion v1, SemanticVersion v2) => !(v1 == v2);
    }
}