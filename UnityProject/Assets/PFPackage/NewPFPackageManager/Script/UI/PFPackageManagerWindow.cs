using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PFPackage
{
    public class PFPackageManagerWindow : EditorWindow
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

        private ToolbarButton descButton;
        private ToolbarButton versionButton;
        private ToolbarButton dependentButton;

        private VisualElement descContainer;
        private VisualElement versionContainer;
        private VisualElement dependentContainer;

        private Label descriptionLabel;

        private int lastSelectedIndex = 0;
        private int selectDetailIndex = 0;


        [MenuItem("Window/NewPFPackageManager", false, 1500)]
        public static async void ShowWindow()
        {
            try
            {
                PFPackageData.I.OnLoadProgress += OnLoadProgress;

                EditorUtility.DisplayProgressBar("加载包管理器", "正在初始化...", 0f);

                await PFPackageData.I.LoadPackagesFromRegistry();

                EditorUtility.ClearProgressBar();

                PFPackageManagerWindow wnd = GetWindow<PFPackageManagerWindow>();
                wnd.minSize = new Vector2(600, 100);
                wnd.titleContent = new GUIContent("PFPackageManager");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"加载包管理器失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"加载包管理器失败: {ex.Message}", "确定");
            }
            finally
            {
                // 取消订阅事件
                PFPackageData.I.OnLoadProgress -= OnLoadProgress;
            }
        }

        private static void OnLoadProgress(int current, int total)
        {
            float progress = total > 0 ? (float)current / total : 0f;
            string progressText = $"正在加载包详情... {current}/{total}";
            EditorUtility.DisplayProgressBar("加载包管理器", progressText, progress);
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            var visualTree = UxmlLoader.LoadWindowUXML<PFPackageManagerWindow>();
            visualTree.CloneTree(root);

            //视图创建
            CreateSearchBar();
            CreatePackageMenu();
            CreatePackageDetail();
            CreatePackageButton();
            CreateBottomContainer();

            //数据刷新
            RefreshPackageList();
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
            var textField = element.Q<Label>("PackageName");
            if (package == null)
                textField.text = "UnKnow";
            else
                textField.text = string.IsNullOrEmpty(package.displayName) ? package.PackageName : package.displayName;
        }

        private void PackageListViewSelectionChanged(IEnumerable<object> obj)
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
            {
                Debug.LogError("[PFPackageManager] 包裹列表选中项为空");
            }
            else
            {
                lastSelectedIndex = packageListView.selectedIndex;
                RefreshPackageDetail();
            }
        }

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
                displayNameLabel.text = selectPackage.displayName;
                versionLabel.text = string.IsNullOrEmpty(selectPackage.localVersion) ? selectPackage.newestVersion : selectPackage.localVersion;
                authorLabel.text = $"By <a href={selectPackage.authorUrl}>{selectPackage.author}</a>";
                packageNameLabel.text = selectPackage.PackageName;
                RefreshDescription();
                RefreshVersion();
                RefreshDependencies();
                RefreshSelectDetailView();
            }
        }

        private void RefreshSelectDetailView()
        {
            if (selectDetailIndex == 0)
                OnDescButtonClick();
            else if (selectDetailIndex == 1)
                OnVersionButtonClick();
            else if (selectDetailIndex == 2)
                OnDependentButtonClick();
        }

        #endregion

        #region 包裹下载卸载

        private void CreatePackageButton()
        {
            installButton = mainContainer.Q<Button>("InstallButton");
            updateButton = mainContainer.Q<Button>("UpdateButton");
            removeButton = mainContainer.Q<Button>("RemoveButton");
            descButton = mainContainer.Q<ToolbarButton>("DescButton");
            versionButton = mainContainer.Q<ToolbarButton>("VersionButton");
            dependentButton = mainContainer.Q<ToolbarButton>("DependentButton");

            installButton.clicked += OnInstallButtonClick;
            updateButton.clicked += OnUpdateButtonClick;
            removeButton.clicked += OnRemoveButtonClick;
            descButton.clicked += OnDescButtonClick;
            versionButton.clicked += OnVersionButtonClick;
            dependentButton.clicked += OnDependentButtonClick;
        }

        private void OnInstallButtonClick()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
                return;
            Debug.Log($"[PFPackageManager] 下载包裹{selectPackage.PackageName}");
        }

        private void OnUpdateButtonClick()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
                return;
            Debug.Log($"[PFPackageManager] 更新包裹{selectPackage.PackageName}");
        }

        private void OnRemoveButtonClick()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
                return;
            Debug.Log($"[PFPackageManager] 卸载包裹{selectPackage.PackageName}");
        }

        private void OnDescButtonClick()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
                return;
            descContainer.style.display = DisplayStyle.Flex;
            versionContainer.style.display = DisplayStyle.None;
            dependentContainer.style.display = DisplayStyle.None;
            selectDetailIndex = 0;
        }

        private void OnVersionButtonClick()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
                return;
            versionContainer.style.display = DisplayStyle.Flex;
            descContainer.style.display = DisplayStyle.None;
            dependentContainer.style.display = DisplayStyle.None;
            selectDetailIndex = 1;
        }

        private void OnDependentButtonClick()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            if (selectPackage == null)
                return;
            dependentContainer.style.display = DisplayStyle.Flex;
            descContainer.style.display = DisplayStyle.None;
            versionContainer.style.display = DisplayStyle.None;
            selectDetailIndex = 2;
        }

        #endregion

        #region Bottom区域

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
            dependentContainer = root.Q<VisualElement>("DependentContainer");
        }
        
        private void RefreshDescription()
        {
            var selectPackage = packageListView.selectedItem as PackageInfo;
            descriptionLabel.text = selectPackage?.description;
        }

        private void RefreshVersion()
        {
            
        }

        private void RefreshDependencies()
        {
            
        }

        #endregion
    }
}