using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PFPackage
{
    public partial class PFPackageManagerWindow
    {
        /// <summary>
        /// 刷新Package菜单
        /// </summary>
        private void RefreshPackageList()
        {
            packageListView.Clear();
            packageListView.ClearSelection();
            packageListView.itemsSource = PFPackageData.I.AllPackages;
            packageListView.Rebuild();
            if (lastSelectedIndex >= 0 && lastSelectedIndex < packageListView.itemsSource.Count)
            {
                packageListView.selectedIndex = lastSelectedIndex;
            }
        }

        /// <summary>
        /// SelectPackage修改之后进行的数据刷新
        /// </summary>
        /// <param name="obj"></param>
        private void PackageListViewSelectionChanged(IEnumerable<object> obj)
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
            {
                PFLog.LogError("包裹列表选中项为空");
            }
            else
            {
                lastSelectedIndex = packageListView.selectedIndex;
                RefreshPackageDetail();
            }
        }

        /// <summary>
        /// 刷新Package详情页
        /// </summary>
        private void RefreshPackageDetail()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
            {
                mainContainer.SetEnabled(false);
            }
            else
            {
                mainContainer.SetEnabled(true);
                RefreshDetails();
                RefreshDescription();
                RefreshVersion();
                RefreshDependencies();
            }
        }

        /// <summary>
        /// 刷新Package 详情
        /// </summary>
        private void RefreshDetails()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null) return;
            displayNameLabel.text = selectPackage.GetDisplayName();
            versionLabel.text = string.IsNullOrEmpty(selectPackage.localVersion) ? selectPackage.newestVersion : selectPackage.localVersion;
            authorLabel.text = $"By <a href={selectPackage.authorUrl}>{selectPackage.author}</a>";
            packageNameLabel.text = selectPackage.PackageName;
            if (selectPackage.IsInstalled)
            {
                installButton.style.display = DisplayStyle.None;
                removeButton.style.display = DisplayStyle.Flex;
                if (selectPackage.HasUpdate)
                    updateButton.style.display = DisplayStyle.Flex;
                else
                    updateButton.style.display = DisplayStyle.None;
            }
            else
            {
                installButton.style.display = DisplayStyle.Flex;
                updateButton.style.display = DisplayStyle.None;
                removeButton.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// 刷新Package 描述页面
        /// </summary>
        private void RefreshDescription()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            descriptionLabel.text = selectPackage?.description;
        }

        /// <summary>
        /// 刷新Package 版本页面
        /// </summary>
        private void RefreshVersion()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null) return;
            var versionItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PFPackageDefine.PACKAGE_MANAGER_ASSETS_PATH + "VersionItem.uxml");
            versionContainer.Clear();
            foreach (var versionData in selectPackage.versions)
            {
                var item = versionItem.Instantiate();
                var foldout = item.Q<Foldout>("Foldout");
                foldout.value = false;

                if (versionData.version == selectPackage.localVersion)
                {
                    foldout.text = $"{versionData.version} (Installed)";
                    foldout.style.color = Color.yellow;
                }
                else if (versionData.version == selectPackage.newestVersion)
                {
                    foldout.text = $"{versionData.version} (Latest)";
                }
                else
                {
                    foldout.text = $"{versionData.version}";
                }

                var publishDate = item.Q<Label>("PublishDate");
                publishDate.text = $"{PFPackageUtils.FormatPublishDate(versionData.publishDate)}";
                var changelog = item.Q<Label>("Changelog");
                changelog.text = versionData.changelog;
                var button = item.Q<Button>("HandlerButton");
                if (versionData.isInstalled)
                {
                    button.text = "Remove";
                    button.clicked += OnRemoveButtonOnClicked;
                }
                else if (selectPackage.IsInstalled)
                {
                    button.text = "Update";
                    button.clicked += () => OnInstallButtonClicked(versionData.version);
                }
                else
                {
                    button.text = "Install";
                    button.clicked += () => OnInstallButtonClicked(versionData.version);
                }
                versionContainer.Add(item);
            }
        }

        /// <summary>
        /// 刷新Package 依赖页面
        /// </summary>
        private async void RefreshDependencies()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null) return;
            var dependencyItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PFPackageDefine.PACKAGE_MANAGER_ASSETS_PATH + "DependencyItem.uxml");
            dependContainer.Clear();
            foreach (var dependency in selectPackage.dependencies)
            {   
                TemplateContainer item = dependencyItem.Instantiate();
                var packageName = item.Q<Label>("PackageName");
                var packageVersion = item.Q<Label>("PackageVersion");
                var status = item.Q<Label>("Status");
                var clickBtn = item.Q<Button>("HandlerButton");

                // 检查依赖状态
                var depStatus = await PFPackageControl.I.CheckDependency(dependency.Key, dependency.Value);

                packageName.text = depStatus.packageName;
                packageVersion.text = $"需求: {depStatus.requiredVersion}";
                status.text = depStatus.GetStatusText();
                status.style.color = depStatus.GetStatusColor();

                // 添加点击跳转功能
                if (depStatus.source == PackageSource.PFPackage && depStatus.relatedPackage != null)
                {
                    // PFPackage：跳转到包详情
                    packageName.AddManipulator(new Clickable(() =>
                    {
                        int index = PFPackageData.I.AllPackages.IndexOf(depStatus.relatedPackage);
                        if (index >= 0)
                        {
                            packageListView.selectedIndex = index;
                            lastSelectedIndex = index;
                            RefreshPackageDetail();
                        }
                    }));
                }
                
                dependContainer.Add(item);
            }
        }

        private async void OnInstallButtonClicked(string version = null)
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null) return;
            await PFPackageControl.I.InstallPackage(selectPackage, version);
            packageListView.Rebuild();
            RefreshPackageDetail();
        }

        private void OnRemoveButtonOnClicked()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null) return;
            bool uninstallSuccess = PFPackageControl.I.UninstallPackage(selectPackage);
            if (uninstallSuccess)
            {
                selectPackage.RefreshStatus();
                packageListView.Rebuild();
                RefreshPackageDetail();
            }
        }
    }
}