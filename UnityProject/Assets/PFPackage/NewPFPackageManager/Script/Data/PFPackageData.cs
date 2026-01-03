using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace PFPackage
{
    public class PFPackageData
    {
        private static PFPackageData instance;
        public static PFPackageData I => instance ??= new PFPackageData();
        
        private List<PackageInfo> allPackages = new List<PackageInfo>();

        public List<PackageInfo> AllPackages => allPackages;

        /// <summary>
        /// 获取所有包的信息
        /// </summary>
        public async Task LoadPackagesFromRegistry()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            allPackages = await PackageLoader.GetAllPackages();
            var semaphore = new System.Threading.SemaphoreSlim(PFPackageConfig.I.MaxConcurrency);

            // 获取所有包的详细信息
            var detailTasks = allPackages.Select(async pkg =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var detail = await PackageLoader.GetPackageDetailAsync(pkg.PackageName);
                    return new { Original = pkg, Detail = detail };
                }
                catch (Exception ex)
                {
                    PFLog.LogError($"加载包 {pkg.PackageName} 详情失败: {ex.Message}");
                    return new { Original = pkg, Detail = (PackageInfo)null };
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            var results = await Task.WhenAll(detailTasks);

            foreach (var result in results)
            {
                MergePackageInfo(result.Original, result.Detail);
            }

            foreach (var pkg in allPackages)
            {
                pkg.RefreshStatus();
            }
            stopwatch.Stop();
            PFLog.Log($"加载所有包耗时 {stopwatch.ElapsedMilliseconds} ms");
        }

        private void MergePackageInfo(PackageInfo target, PackageInfo source)
        {
            if (target == null || source == null) return;

            target.displayName = source.displayName ?? target.displayName;
            target.description = source.description ?? target.description;
            target.author = source.author ?? target.author;
            target.authorUrl = source.authorUrl ?? target.authorUrl;
            target.documentationUrl = source.documentationUrl ?? target.documentationUrl;
            target.changelogUrl = source.changelogUrl ?? target.changelogUrl;

            if (source.versions != null && source.versions.Count > 0)
                target.versions = source.versions;

            if (source.dependencies != null)
                target.dependencies = source.dependencies;

            if (!string.IsNullOrEmpty(source.newestVersion))
                target.newestVersion = source.newestVersion;
        }
    }
}