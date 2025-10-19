using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 包数据管理器 - 负责加载和管理包数据
    /// </summary>
    public class PackageDataManager
    {
        private readonly PFRegistryClient registryClient;
        private readonly PFPackageInstaller installer;
        private List<PackageInfo> allPackages = new List<PackageInfo>();

        public event Action<List<PackageInfo>> OnPackagesLoaded;
        public event Action<PackageInfo> OnPackageDetailUpdated;

        public PackageDataManager(PFRegistryClient registryClient, PFPackageInstaller installer)
        {
            this.registryClient = registryClient;
            this.installer = installer;
        }

        /// <summary>
        /// 获取所有包
        /// </summary>
        public List<PackageInfo> GetAllPackages()
        {
            return allPackages;
        }

        /// <summary>
        /// 从 Registry 加载包列表
        /// </summary>
        public void LoadPackagesFromRegistry()
        {
            registryClient.GetAllPackages(
                onSuccess: (packages) =>
                {
                    allPackages = packages;
                    Debug.Log($"成功加载 {packages.Count} 个包");

                    // 检测本地已安装的包
                    UpdateInstalledStatus();

                    // 为每个包加载详细信息
                    LoadPackageDetails();
                },
                onError: (error) =>
                {
                    Debug.LogError($"加载包列表失败: {error}");
                    allPackages = new List<PackageInfo>();
                    OnPackagesLoaded?.Invoke(allPackages);
                }
            );
        }

        /// <summary>
        /// 更新已安装状态
        /// </summary>
        private void UpdateInstalledStatus()
        {
            // 刷新Unity包缓存
            UnityPackageDependencyChecker.RefreshCache();

            foreach (var pkg in allPackages)
            {
                pkg.isInstalled = installer.IsPackageInstalled(pkg.name);
                if (pkg.isInstalled)
                {
                    pkg.localVersion = installer.GetInstalledVersion(pkg.name);
                    pkg.hasUpdate = PFPackageOperationManager.CompareVersions(pkg.version, pkg.localVersion) > 0;
                }
            }
        }

        /// <summary>
        /// 加载所有包的详细信息
        /// </summary>
        private void LoadPackageDetails()
        {
            int loadedCount = 0;
            int totalCount = allPackages.Count;

            foreach (var pkg in allPackages)
            {
                LoadPackageDetail(pkg, () =>
                {
                    loadedCount++;
                    if (loadedCount >= totalCount)
                    {
                        // 所有包详情加载完成
                        OnPackagesLoaded?.Invoke(allPackages);
                    }
                });
            }
        }

        /// <summary>
        /// 加载单个包的详细信息
        /// </summary>
        private void LoadPackageDetail(PackageInfo package, Action onComplete)
        {
            registryClient.GetPackageDetail(package.name,
                onSuccess: (detailedPkg) =>
                {
                    // 更新包信息
                    package.displayName = detailedPkg.displayName;
                    package.versions = detailedPkg.versions;
                    package.authorUrl = detailedPkg.authorUrl;
                    package.dependencies = detailedPkg.dependencies;

                    // 同步版本列表的 isInstalled 状态
                    if (package.isInstalled && !string.IsNullOrEmpty(package.localVersion))
                    {
                        foreach (var ver in package.versions)
                        {
                            ver.isInstalled = (ver.version == package.localVersion);
                        }
                    }

                    OnPackageDetailUpdated?.Invoke(package);
                    onComplete?.Invoke();
                },
                onError: (error) =>
                {
                    Debug.LogWarning($"加载 {package.name} 详情失败: {error}");
                    onComplete?.Invoke();
                }
            );
        }
    }
}