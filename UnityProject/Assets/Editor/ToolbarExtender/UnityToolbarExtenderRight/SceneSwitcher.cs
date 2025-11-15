#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;

namespace TEngine
{
    [System.Serializable]
    public class SceneEntry
    {
        public string scenePath = "";
        public string sceneName => Path.GetFileNameWithoutExtension(scenePath);
    }

    /// <summary>
    /// SceneSwitcher
    /// </summary>
    public partial class UnityToolbarExtenderRight
    {
        private static List<SceneEntry> m_AllScenes = new List<SceneEntry>();
        private static List<string> m_BookmarkedScenePaths = new List<string>();
        private static ToolbarSceneSelector m_SceneSelector;


        static void UpdateScenes()
        {
            m_AllScenes.Clear();

            // 获取项目中所有场景文件
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            m_AllScenes = guids.Select(guid => new SceneEntry { scenePath = AssetDatabase.GUIDToAssetPath(guid) })
                             .OrderBy(entry => entry.sceneName)
                             .ToList();

            // 加载收藏场景数据
            LoadBookmarkedScenes();
        }

        static void LoadBookmarkedScenes()
        {
            string bookmarksData = EditorPrefs.GetString("ToolbarSceneSelector.BookmarkedScenes", "");
            if (!string.IsNullOrEmpty(bookmarksData))
            {
                m_BookmarkedScenePaths = bookmarksData.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToList();
            }
        }

        static void SaveBookmarkedScenes()
        {
            string bookmarksData = string.Join(";", m_BookmarkedScenePaths);
            EditorPrefs.SetString("ToolbarSceneSelector.BookmarkedScenes", bookmarksData);
        }

        static void OnToolbarGUI_SceneSwitch()
        {
            // 如果没有场景，直接返回
            if (m_AllScenes.Count == 0)
                return;

            // 获取当前场景名称
            string currentSceneName = SceneManager.GetActiveScene().name;
            EditorGUILayout.LabelField("场景:", GUILayout.Width(32));

            // 使用 GUI.skin.button.CalcSize 计算文本的精确宽度
            GUIContent content = new GUIContent(currentSceneName + " ▼");
            Vector2 textSize = GUI.skin.button.CalcSize(content);

            // 设置按钮宽度为文本的宽度，并限制最大值
            float buttonWidth = Mathf.Min(textSize.x, 150f);

            // 自定义GUIStyle
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };

            // 在工具栏中显示选择器
            Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(currentSceneName + " ▼"), buttonStyle, GUILayout.Width(buttonWidth));
            if (GUI.Button(buttonRect, currentSceneName + " ▼", buttonStyle))
            {
                ShowSceneSelector(buttonRect);
            }
        }

        static void ShowSceneSelector(Rect buttonRect)
        {
            if (m_SceneSelector == null)
                m_SceneSelector = ScriptableObject.CreateInstance<ToolbarSceneSelector>();

            m_SceneSelector.ShowPopup();
            m_SceneSelector.Focus();

            // 将按钮位置转换为屏幕坐标
            Vector2 buttonScreenPos = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y));

            // 设置窗口位置（在按钮下方）
            float windowWidth = 190;
            float windowHeight = 300;
            float xPos = buttonScreenPos.x;
            float yPos = buttonScreenPos.y + buttonRect.height + 2; // 按钮下方留2像素间距

            // 确保窗口不会超出屏幕边界
            if (xPos + windowWidth > Screen.currentResolution.width)
                xPos = Screen.currentResolution.width - windowWidth;
            if (xPos < 0)
                xPos = 0;

            m_SceneSelector.position = new Rect(xPos, yPos, windowWidth, windowHeight);
        }

        public static List<SceneEntry> GetAllScenes() => m_AllScenes;

        public static List<string> GetBookmarkedScenePaths() => m_BookmarkedScenePaths;

        public static void ToggleBookmark(string scenePath)
        {
            if (m_BookmarkedScenePaths.Contains(scenePath))
                m_BookmarkedScenePaths.Remove(scenePath);
            else
                m_BookmarkedScenePaths.Add(scenePath);

            SaveBookmarkedScenes();
        }

        public static void SwitchScene(string scenePath)
        {
            // 保存当前场景的修改
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 切换到新场景
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    public class ToolbarSceneSelector : EditorWindow
    {
        private string searchString = "";
        private UnityEditor.IMGUI.Controls.SearchField searchField;
        private Vector2 scrollPos;
        private List<SceneEntry> searchedEntries = new List<SceneEntry>();
        private List<SceneEntry> bookmarkedEntries = new List<SceneEntry>();
        private int keyboardFocusedRowIndex = -1;
        private SceneEntry keyboardFocusedEntry;

        void OnEnable()
        {
            UpdateEntries();
        }

        void UpdateEntries()
        {
            var allScenes = UnityToolbarExtenderRight.GetAllScenes();
            var bookmarkedPaths = UnityToolbarExtenderRight.GetBookmarkedScenePaths();

            bookmarkedEntries = allScenes.Where(entry => bookmarkedPaths.Contains(entry.scenePath)).ToList();

            UpdateSearch();
        }

        void UpdateSearch()
        {
            var allScenes = UnityToolbarExtenderRight.GetAllScenes();

            if (string.IsNullOrEmpty(searchString))
            {
                searchedEntries = allScenes.Where(entry => !bookmarkedEntries.Contains(entry)).ToList();
            }
            else
            {
                searchedEntries = allScenes.Where(entry =>
                    entry.sceneName.ToLower().Contains(searchString.ToLower())).ToList();
            }
        }

        void OnGUI()
        {
            HandleKeyboard();
            DrawSearchField();
            DrawSceneList();
        }

        void HandleKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Return)
                {
                    if (keyboardFocusedEntry != null)
                    {
                        UnityToolbarExtenderRight.SwitchScene(keyboardFocusedEntry.scenePath);
                        Close();
                        Event.current.Use();
                    }
                }
                else if (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow)
                {
                    int totalRows = (string.IsNullOrEmpty(searchString) ? bookmarkedEntries.Count + searchedEntries.Count : searchedEntries.Count);
                    if (totalRows > 0)
                    {
                        if (Event.current.keyCode == KeyCode.UpArrow)
                            keyboardFocusedRowIndex = (keyboardFocusedRowIndex - 1 + totalRows) % totalRows;
                        else
                            keyboardFocusedRowIndex = (keyboardFocusedRowIndex + 1) % totalRows;

                        UpdateFocusedEntry();
                        Event.current.Use();
                    }
                }
            }
        }

        void UpdateFocusedEntry()
        {
            if (string.IsNullOrEmpty(searchString))
            {
                if (keyboardFocusedRowIndex < bookmarkedEntries.Count)
                    keyboardFocusedEntry = bookmarkedEntries[keyboardFocusedRowIndex];
                else if (keyboardFocusedRowIndex - bookmarkedEntries.Count < searchedEntries.Count)
                    keyboardFocusedEntry = searchedEntries[keyboardFocusedRowIndex - bookmarkedEntries.Count];
            }
            else
            {
                if (keyboardFocusedRowIndex < searchedEntries.Count)
                    keyboardFocusedEntry = searchedEntries[keyboardFocusedRowIndex];
            }
        }

        void DrawSearchField()
        {
            if (searchField == null)
            {
                searchField = new UnityEditor.IMGUI.Controls.SearchField();
                searchField.SetFocus();
            }

            EditorGUI.BeginChangeCheck();
            searchString = searchField.OnGUI(new Rect(5, 5, position.width - 10, 18), searchString);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSearch();
                keyboardFocusedRowIndex = 0;
                UpdateFocusedEntry();
            }
        }

        void DrawSceneList()
        {
            Rect scrollRect = new Rect(0, 25, position.width, position.height - 25);

            scrollPos = GUI.BeginScrollView(scrollRect, scrollPos, new Rect(0, 0, position.width - 20, GetContentHeight()));

            float yPos = 0;
            int rowIndex = 0;

            // 绘制收藏的场景
            if (string.IsNullOrEmpty(searchString) && bookmarkedEntries.Count > 0)
            {
                foreach (var entry in bookmarkedEntries)
                {
                    DrawSceneRow(entry, yPos, rowIndex == keyboardFocusedRowIndex, true);
                    yPos += 20;
                    rowIndex++;
                }

                // 分隔线
                if (searchedEntries.Count > 0)
                {
                    GUI.color = Color.gray;
                    GUI.DrawTexture(new Rect(10, yPos + 5, position.width - 20, 1), EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                    yPos += 15;
                }
            }

            // 绘制其他场景
            foreach (var entry in searchedEntries)
            {
                bool isBookmarked = bookmarkedEntries.Contains(entry);
                DrawSceneRow(entry, yPos, rowIndex == keyboardFocusedRowIndex, isBookmarked);
                yPos += 20;
                rowIndex++;
            }

            GUI.EndScrollView();
        }

        void DrawSceneRow(SceneEntry entry, float yPos, bool isFocused, bool isBookmarked)
        {
            Rect rowRect = new Rect(0, yPos, position.width - 20, 20);
            bool isHovered = rowRect.Contains(Event.current.mousePosition);

            // 背景
            if (isFocused)
            {
                GUI.color = new Color(0.3f, 0.5f, 0.85f, 0.8f);
                GUI.DrawTexture(rowRect, EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
            }
            else if (isHovered)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                GUI.DrawTexture(rowRect, EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
            }

            // 场景图标
            Rect iconRect = new Rect(5, yPos + 2, 16, 16);
            GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("SceneAsset Icon").image);

            // 场景名称
            Rect nameRect = new Rect(25, yPos, position.width - 70, 20);

            // 高亮当前场景
            if (entry.scenePath == SceneManager.GetActiveScene().path)
            {
                GUI.contentColor = Color.yellow;
                EditorGUI.LabelField(nameRect, entry.sceneName, EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
            }
            else
            {
                EditorGUI.LabelField(nameRect, entry.sceneName);
            }

            // 收藏按钮 - 只在悬停或者已收藏且未聚焦时显示
            bool showStarButton = isHovered || (isBookmarked && !isFocused);
            if (showStarButton)
            {
                Rect starRect = new Rect(position.width - 40, yPos + 2, 16, 16);
                // vHierarchy 的逻辑：如果已收藏且鼠标悬停在按钮上，显示空心；否则显示实心
                bool starRectHovered = starRect.Contains(Event.current.mousePosition);
                string starIcon = (isBookmarked ^ starRectHovered) ? "Favorite Icon" : "Favorite";

                if (GUI.Button(starRect, EditorGUIUtility.IconContent(starIcon), GUIStyle.none))
                {
                    UnityToolbarExtenderRight.ToggleBookmark(entry.scenePath);
                    UpdateEntries();
                }
            }

            // 点击切换场景
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                UnityToolbarExtenderRight.SwitchScene(entry.scenePath);
                Close();
                Event.current.Use();
            }
        }

        float GetContentHeight()
        {
            int totalRows = string.IsNullOrEmpty(searchString) ?
                bookmarkedEntries.Count + searchedEntries.Count :
                searchedEntries.Count;

            float height = totalRows * 20;

            // 为分隔线增加高度
            if (string.IsNullOrEmpty(searchString) && bookmarkedEntries.Count > 0 && searchedEntries.Count > 0)
                height += 15;

            return height;
        }

        void OnLostFocus()
        {
            Close();
        }
    }
}
#endif
