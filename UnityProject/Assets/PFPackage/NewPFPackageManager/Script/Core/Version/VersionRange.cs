using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PFPackage
{
    /// <summary>
    /// 版本范围解析器（支持 NPM 风格的版本范围）
    /// 支持: ^, ~, >, <, >=, <=, x, *, 范围组合等
    /// </summary>
    public class VersionRange
    {
        private readonly string rangeExpression;

        public VersionRange(string range)
        {
            this.rangeExpression = range?.Trim() ?? "*";
        }

        /// <summary>
        /// 检查版本是否满足范围要求
        /// </summary>
        public bool IsSatisfiedBy(string versionString)
        {
            if (!SemanticVersion.TryParse(versionString, out var version))
                return false;

            return IsSatisfiedBy(version);
        }

        /// <summary>
        /// 检查版本是否满足范围要求
        /// </summary>
        public bool IsSatisfiedBy(SemanticVersion version)
        {
            // 处理 "||" 或运算
            if (rangeExpression.Contains("||"))
            {
                var orParts = rangeExpression.Split(new[] { "||" }, StringSplitOptions.None);
                return orParts.Any(part => new VersionRange(part).IsSatisfiedBy(version));
            }

            // 处理空格分隔的 AND 条件（例如 ">=1.0.0 <2.0.0"）
            var andParts = rangeExpression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (andParts.Length > 1)
            {
                return andParts.All(part => new VersionRange(part).IsSatisfiedBy(version));
            }

            // 单个条件
            return MatchSingleCondition(version, rangeExpression);
        }

        /// <summary>
        /// 从可用版本列表中选择最佳版本
        /// </summary>
        public SemanticVersion SelectBestVersion(List<SemanticVersion> availableVersions)
        {
            // 过滤出满足范围的版本
            var matchedVersions = availableVersions.Where(IsSatisfiedBy).ToList();

            if (matchedVersions.Count == 0)
                return null;

            // 返回最高版本
            matchedVersions.Sort();
            return matchedVersions.Last();
        }

        /// <summary>
        /// 从字符串版本列表中选择最佳版本
        /// </summary>
        public string SelectBestVersion(List<string> availableVersions)
        {
            var semanticVersions = availableVersions
                .Select(v => SemanticVersion.TryParse(v, out var sv) ? sv : null)
                .Where(v => v != null)
                .ToList();

            var best = SelectBestVersion(semanticVersions);
            return best?.ToString();
        }

        /// <summary>
        /// 匹配单个条件
        /// </summary>
        private bool MatchSingleCondition(SemanticVersion version, string condition)
        {
            condition = condition.Trim();

            // 1. 任意版本
            if (condition == "*" || condition == "")
                return true;

            // 2. 插入符 ^（推荐用法）
            if (condition.StartsWith("^"))
            {
                return MatchCaretRange(version, condition.Substring(1));
            }

            // 3. 波浪号 ~
            if (condition.StartsWith("~"))
            {
                return MatchTildeRange(version, condition.Substring(1));
            }

            // 4. 比较运算符
            if (condition.StartsWith(">="))
            {
                var minVersion = SemanticVersion.Parse(condition.Substring(2).Trim());
                return version >= minVersion;
            }
            if (condition.StartsWith("<="))
            {
                var maxVersion = SemanticVersion.Parse(condition.Substring(2).Trim());
                return version <= maxVersion;
            }
            if (condition.StartsWith(">"))
            {
                var minVersion = SemanticVersion.Parse(condition.Substring(1).Trim());
                return version > minVersion;
            }
            if (condition.StartsWith("<"))
            {
                var maxVersion = SemanticVersion.Parse(condition.Substring(1).Trim());
                return version < maxVersion;
            }

            // 5. 连字符范围 "1.2.3 - 2.3.4"
            if (condition.Contains(" - "))
            {
                var parts = condition.Split(new[] { " - " }, StringSplitOptions.None);
                var minVersion = SemanticVersion.Parse(parts[0].Trim());
                var maxVersion = SemanticVersion.Parse(parts[1].Trim());
                return version >= minVersion && version <= maxVersion;
            }

            // 6. 通配符 x 或 *
            if (condition.Contains("x") || condition.Contains("X") || condition.Contains("*"))
            {
                return MatchWildcard(version, condition);
            }

            // 7. 精确版本
            var exactVersion = SemanticVersion.Parse(condition);
            return version == exactVersion;
        }

        /// <summary>
        /// 匹配插入符范围 ^
        /// ^1.2.3 := >=1.2.3 <2.0.0
        /// ^0.2.3 := >=0.2.3 <0.3.0（0.x 特殊处理）
        /// ^0.0.3 := =0.0.3（0.0.x 不允许更新）
        /// </summary>
        private bool MatchCaretRange(SemanticVersion version, string baseVersionStr)
        {
            var baseVersion = SemanticVersion.Parse(baseVersionStr);

            // 版本必须 >= 基础版本
            if (version < baseVersion)
                return false;

            // 0.0.x 系列：只允许精确匹配
            if (baseVersion.Major == 0 && baseVersion.Minor == 0)
            {
                return version.Major == 0 && version.Minor == 0 && version.Patch == baseVersion.Patch;
            }

            // 0.x 系列：允许 Patch 更新，但不允许 Minor 更新
            if (baseVersion.Major == 0)
            {
                return version.Major == 0 && version.Minor == baseVersion.Minor;
            }

            // 1.x 及以上：允许 Minor 和 Patch 更新，但不允许 Major 更新
            return version.Major == baseVersion.Major;
        }

        /// <summary>
        /// 匹配波浪号范围 ~
        /// ~1.2.3 := >=1.2.3 <1.3.0
        /// ~1.2 := >=1.2.0 <1.3.0
        /// ~1 := >=1.0.0 <2.0.0
        /// </summary>
        private bool MatchTildeRange(SemanticVersion version, string baseVersionStr)
        {
            // 处理不完整版本号
            var parts = baseVersionStr.Split('.');
            if (parts.Length == 1)
            {
                // ~1 := >=1.0.0 <2.0.0
                var major = int.Parse(parts[0]);
                return version.Major == major;
            }
            else if (parts.Length == 2)
            {
                // ~1.2 := >=1.2.0 <1.3.0
                var major = int.Parse(parts[0]);
                var minor = int.Parse(parts[1]);
                return version.Major == major && version.Minor == minor;
            }
            else
            {
                // ~1.2.3 := >=1.2.3 <1.3.0
                var baseVersion = SemanticVersion.Parse(baseVersionStr);
                return version >= baseVersion &&
                       version.Major == baseVersion.Major &&
                       version.Minor == baseVersion.Minor;
            }
        }

        /// <summary>
        /// 匹配通配符
        /// 1.2.x := >=1.2.0 <1.3.0
        /// 1.x := >=1.0.0 <2.0.0
        /// * := 任意版本
        /// </summary>
        private bool MatchWildcard(SemanticVersion version, string pattern)
        {
            pattern = pattern.Replace("x", "*").Replace("X", "*");

            if (pattern == "*")
                return true;

            var parts = pattern.Split('.');

            // 1.2.* := >=1.2.0 <1.3.0
            if (parts.Length == 3 && parts[2] == "*")
            {
                var major = int.Parse(parts[0]);
                var minor = int.Parse(parts[1]);
                return version.Major == major && version.Minor == minor;
            }

            // 1.* := >=1.0.0 <2.0.0
            if (parts.Length >= 2 && parts[1] == "*")
            {
                var major = int.Parse(parts[0]);
                return version.Major == major;
            }

            return false;
        }

        public override string ToString()
        {
            return rangeExpression;
        }
    }
}