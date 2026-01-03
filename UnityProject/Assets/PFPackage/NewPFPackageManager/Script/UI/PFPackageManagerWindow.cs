using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PFPackage
{
    public partial class PFPackageManagerWindow : EditorWindow
    {
        private VisualElement root;
        private ListView packageListView;
        private VisualElement mainContainer;
        private Label displayNameLabel;
        private Label versionLabel;
        private Label authorLabel;
        private Label packageNameLabel;

        private Button installButton;
        private Button updateButton;
        private Button removeButton;

        private ToggleButtonGroup buttonGroup;

        private VisualElement descContainer;
        private VisualElement versionContainer;
        private VisualElement dependContainer;

        private Label descriptionLabel;

        private int lastSelectedIndex = 0;


        [MenuItem("Window/NewPFPackageManager", false, 1500)]
        public static async void ShowWindow()
        {
            PFPackageManagerWindow wnd = GetWindow<PFPackageManagerWindow>();
            wnd.minSize = new Vector2(600, 100);
            wnd.titleContent = new GUIContent("PFPackageManager");
        }

        private static void OnLoadProgress(int current, int total)
        {
            float progress = total > 0 ? (float)current / total : 0f;
            string progressText = $"正在加载包详情... {current}/{total}";
            EditorUtility.DisplayProgressBar("加载包管理器", progressText, progress);
        }

        public async void CreateGUI()
        {
            try
            {
                await PFPackageData.I.LoadPackagesFromRegistry();
            }
            catch (System.Exception ex)
            {
                PFLog.LogError($"加载包管理器失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"加载包管理器失败: {ex.Message}", "确定");
                return;
            }
            
            root = rootVisualElement;
            var visualTree = UxmlLoader.LoadWindowUXML<PFPackageManagerWindow>();
            visualTree.CloneTree(root);

            //视图创建
            CreateSearchBar(); //搜索栏
            CreatePackageMenu(); //包列表
            CreatePackageDetail(); //包详情
            CreatePackageButton(); //下载更新卸载按钮
            CreateBottomContainer(); //详情,版本,依赖

            //数据刷新
            RefreshPackageList();
            buttonGroup.SetSelectedIndex(0); //默认在第一个页签
        }

        #region 搜索栏

        /// <summary>
        /// 创建搜索栏
        /// </summary>
        private void CreateSearchBar()
        {
            var searchBar = root.Q<ToolbarSearchField>("SearchBar");
            searchBar.RegisterValueChangedCallback(OnSearchKeyWordChange);
        }

        private void OnSearchKeyWordChange(ChangeEvent<string> evt)
        {
            string searchText = evt.newValue?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                // 如果搜索框为空，显示所有包
                packageListView.itemsSource = PFPackageData.I.AllPackages;
            }
            else
            {
                // 过滤包列表
                var filteredPackages = PFPackageData.I.AllPackages.FindAll(pkg =>
                    IsPackageMatch(pkg, searchText));

                packageListView.itemsSource = filteredPackages;
            }

            // 重建列表并保持选中状态
            packageListView.Rebuild();

            // 尝试保持之前的选中项
            if (lastSelectedIndex >= 0 && lastSelectedIndex < packageListView.itemsSource.Count)
            {
                packageListView.selectedIndex = lastSelectedIndex;
            }
            else if (packageListView.itemsSource.Count > 0)
            {
                packageListView.selectedIndex = 0;
            }

            PackageListViewSelectionChanged(null);
        }

        private bool IsPackageMatch(PackageInfo package, string searchText)
        {
            if (package == null || string.IsNullOrEmpty(searchText))
                return false;

            // 搜索包名
            if (!string.IsNullOrEmpty(package.PackageName) &&
                package.PackageName.ToLower().Contains(searchText))
                return true;

            // 搜索显示名
            if (!string.IsNullOrEmpty(package.displayName) &&
                package.displayName.ToLower().Contains(searchText))
                return true;

            // 搜索描述
            if (!string.IsNullOrEmpty(package.description) &&
                package.description.ToLower().Contains(searchText))
                return true;

            // 搜索作者
            if (!string.IsNullOrEmpty(package.author) &&
                package.author.ToLower().Contains(searchText))
                return true;

            // 搜索标签（如果有的话）
            if (!string.IsNullOrEmpty(package.newestVersion) &&
                package.newestVersion.ToLower().Contains(searchText))
                return true;

            return false;
        }

        #endregion

        #region 包裹列表

        /// <summary>
        /// 创建包裹列表
        /// </summary>
        private void CreatePackageMenu()
        {
            packageListView = root.Q<ListView>("PackageMenuList");
            packageListView.makeItem = MakePackageListViewItem;
            packageListView.bindItem = BindPackageListViewItem;
            packageListView.selectionChanged += PackageListViewSelectionChanged;
        }

        private VisualElement MakePackageListViewItem()
        {
            var packageItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PFPackageDefine.PACKAGE_MANAGER_ASSETS_PATH + "PackageItem.uxml");
            return packageItem.Instantiate();
        }

        private void BindPackageListViewItem(VisualElement element, int index)
        {
            var package = packageListView.itemsSource[index] as PackageInfo;
            if (package == null) return;

            var textField = element.Q<Label>("PackageName");
            textField.text = package.GetDisplayName();
            var installedIcon = element.Q<Image>("InstalledIcon");
            var upgradeIcon = element.Q<Image>("UpgradeIcon");
            if (package.IsInstalled)
            {
                if (package.HasUpdate)
                {
                    installedIcon.style.display = DisplayStyle.None;
                    upgradeIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    installedIcon.style.display = DisplayStyle.Flex;
                    upgradeIcon.style.display = DisplayStyle.None;
                }
            }
            else
            {
                installedIcon.style.display = DisplayStyle.None;
                upgradeIcon.style.display = DisplayStyle.None;
            }
        }

        #endregion

        #region 包裹详情

        private void CreatePackageDetail()
        {
            mainContainer = root.Q<VisualElement>("MainContainer");
            displayNameLabel = mainContainer.Q<Label>("DisplayName");
            versionLabel = mainContainer.Q<Label>("PackageVersion");
            authorLabel = mainContainer.Q<Label>("PackageAuthor");
            packageNameLabel = mainContainer.Q<Label>("PackageName");
        }

        private void CreateBottomContainer()
        {
            CreateDescContainer();
            CreateVersionContainer();
            CreateDependentContainer();
        }

        private void CreateDescContainer()
        {
            descContainer = root.Q<VisualElement>("DescContainer");
            descriptionLabel = descContainer.Q<Label>("Description");
        }

        private void CreateVersionContainer()
        {
            versionContainer = root.Q<VisualElement>("VersionContainer");
        }

        private void CreateDependentContainer()
        {
            dependContainer = root.Q<VisualElement>("DependentContainer");
        }

        #endregion

        #region 包裹下载卸载

        private void CreatePackageButton()
        {
            installButton = mainContainer.Q<Button>("InstallButton");
            updateButton = mainContainer.Q<Button>("UpdateButton");
            removeButton = mainContainer.Q<Button>("RemoveButton");
            buttonGroup = mainContainer.Q<ToggleButtonGroup>("DetailButtonGroup");

            installButton.clicked += () => OnInstallButtonClicked();
            updateButton.clicked += () => OnInstallButtonClicked();
            removeButton.clicked += OnRemoveButtonOnClicked;
            buttonGroup.onSelectedIndexChanged += OnDetailButtonClick;
        }

        private void OnDetailButtonClick(ChangeEvent<int> evt)
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
                return;
            if (evt.newValue == 0) //Desc
            {
                descContainer.style.display = DisplayStyle.Flex;
                versionContainer.style.display = DisplayStyle.None;
                dependContainer.style.display = DisplayStyle.None;
            }
            else if (evt.newValue == 1) //Version
            {
                versionContainer.style.display = DisplayStyle.Flex;
                descContainer.style.display = DisplayStyle.None;
                dependContainer.style.display = DisplayStyle.None;
            }
            else if (evt.newValue == 2) //Dependence
            {
                dependContainer.style.display = DisplayStyle.Flex;
                descContainer.style.display = DisplayStyle.None;
                versionContainer.style.display = DisplayStyle.None;
            }
        }

        #endregion
    }
}