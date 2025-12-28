using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace PFPackage
{
    public class PFPackageData 
    {
        private static PFPackageData instance;
        public static PFPackageData I
        {
            get
            {
                if (instance == null)
                {
                    instance = new PFPackageData();
                }
                return instance;
            }
        }
        
        private readonly PackageLoader packageLoader = new();
        private List<PackageInfo> allPackages = new List<PackageInfo>();

        // 进度报告事件
        public event Action<int, int> OnLoadProgress; 

        public List<PackageInfo> AllPackages => allPackages;

        public async Task LoadPackagesFromRegistry()
        {
            allPackages = await packageLoader.GetAllPackages();

            //TODO UpdateInstalledStatus()

            int completedCount = 0;
            int totalCount = allPackages.Count;

            // 获取所有包的详细信息
            var detailTasks = allPackages.Select(async pkg =>
            {
                try
                {
                    var detail = await packageLoader.GetPackageDetailAsync(pkg.PackageName);
                    
                    // 更新进度
                    var current = System.Threading.Interlocked.Increment(ref completedCount);
                    OnLoadProgress?.Invoke(current, totalCount);
                    
                    return new { Original = pkg, Detail = detail };
                }
                catch (Exception ex)
                {
                    // 即使失败也要更新进度
                    var current = System.Threading.Interlocked.Increment(ref completedCount);
                    OnLoadProgress?.Invoke(current, totalCount);
                    
                    Debug.LogWarning($"加载包 {pkg.PackageName} 详情失败: {ex.Message}");
                    return new { Original = pkg, Detail = (PackageInfo)null };
                }
            }).ToArray();

            var results = await Task.WhenAll(detailTasks);

            foreach (var result in results)
            {
                MergePackageInfo(result.Original, result.Detail);
            }
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