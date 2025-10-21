using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 依赖冲突检测器
    /// 用于检测多个包对同一依赖的版本要求是否冲突
    /// </summary>
    public class DependencyConflictDetector
    {
        /// <summary>
        /// 检测依赖冲突
        /// </summary>
        /// <param name="packageToInstall">准备安装的包</param>
        /// <param name="allPackages">所有可用包列表</param>
        /// <param name="installer">安装器（用于检查已安装的包）</param>
        /// <returns>冲突列表</returns>
        public static List<DependencyConflict> DetectConflicts(
            PackageInfo packageToInstall,
            List<PackageInfo> allPackages,
            PFPackageInstaller installer)
        {
            var conflicts = new List<DependencyConflict>();

            // 收集所有已安装包的依赖
            var installedPackages = allPackages.Where(p => p.isInstalled).ToList();

            // 遍历准备安装的包的依赖
            foreach (var newDep in packageToInstall.dependencies)
            {
                string depName = newDep.Key;
                string newRequirement = newDep.Value;

                // 跳过 Unity 官方包
                if (depName.StartsWith("com.unity."))
                    continue;

                // 查找已安装的包中是否也依赖这个包
                foreach (var installedPkg in installedPackages)
                {
                    if (installedPkg.dependencies != null && installedPkg.dependencies.ContainsKey(depName))
                    {
                        string existingRequirement = installedPkg.dependencies[depName];

                        // 检查两个版本要求是否兼容
                        if (!AreRequirementsCompatible(newRequirement, existingRequirement, allPackages, depName))
                        {
                            conflicts.Add(new DependencyConflict
                            {
                                dependencyName = depName,
                                package1 = packageToInstall.name,
                                package1Requirement = newRequirement,
                                package2 = installedPkg.name,
                                package2Requirement = existingRequirement
                            });
                        }
                    }
                }

                // 检查是否已安装了这个依赖，但版本不兼容
                if (installer.IsPackageInstalled(depName))
                {
                    string installedVersion = installer.GetInstalledVersion(depName);
                    var versionRange = new VersionRange(newRequirement);
                    if (!versionRange.IsSatisfiedBy(installedVersion))
                    {
                        conflicts.Add(new DependencyConflict
                        {
                            dependencyName = depName,
                            package1 = packageToInstall.name,
                            package1Requirement = newRequirement,
                            package2 = $"{depName} (已安装)",
                            package2Requirement = installedVersion
                        });
                    }
                }
            }

            return conflicts;
        }

        /// <summary>
        /// 检查两个版本要求是否兼容（是否存在交集）
        /// </summary>
        private static bool AreRequirementsCompatible(
            string requirement1,
            string requirement2,
            List<PackageInfo> allPackages,
            string depName)
        {
            // 查找依赖包
            var depPackage = allPackages.Find(p => p.name == depName);
            if (depPackage == null)
                return false; // 找不到包，算不兼容

            // 收集所有可用版本
            var availableVersions = new List<string> { depPackage.version };
            if (depPackage.versions != null && depPackage.versions.Count > 0)
            {
                availableVersions.AddRange(depPackage.versions.Select(v => v.version));
            }

            var range1 = new VersionRange(requirement1);
            var range2 = new VersionRange(requirement2);

            // 检查是否存在同时满足两个要求的版本
            foreach (var version in availableVersions)
            {
                if (range1.IsSatisfiedBy(version) && range2.IsSatisfiedBy(version))
                {
                    return true; // 找到兼容版本
                }
            }

            return false; // 没有兼容版本
        }

        /// <summary>
        /// 生成冲突报告
        /// </summary>
        public static string GenerateConflictReport(List<DependencyConflict> conflicts)
        {
            if (conflicts.Count == 0)
                return "";

            var report = "检测到依赖冲突:\n\n";
            foreach (var conflict in conflicts)
            {
                report += $"❌ {conflict.dependencyName}\n";
                report += $" · {conflict.package1} 需要: {conflict.package1Requirement}\n";
                report += $" · {conflict.package2} 需要: {conflict.package2Requirement}\n";
                report += "\n";
            }

            report += "这些冲突无法自动解决，可能导致运行时错误。\n";
            report += "建议:\n";
            report += "1. 检查包的版本兼容性\n";
            report += "2. 升级或降级某个包以解决冲突\n";
            report += "3. 联系包作者报告兼容性问题";

            return report;
        }
    }

    /// <summary>
    /// 依赖冲突信息
    /// </summary>
    public class DependencyConflict
    {
        public string dependencyName; // 冲突的依赖包名
        public string package1; // 包1的名称
        public string package1Requirement; // 包1的版本要求
        public string package2; // 包2的名称
        public string package2Requirement; // 包2的版本要求
    }
}