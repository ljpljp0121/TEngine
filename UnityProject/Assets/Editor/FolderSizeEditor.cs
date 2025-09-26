using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在Project窗口文件大小
/// </summary>
[InitializeOnLoad]
public class FolderSizeEditor
{
    #region 常量定义

    private const float CACHE_CLEANUP_INTERVAL = 30f;
    private const int GRID_VIEW_HEIGHT_THRESHOLD = 20;
    private const int GRID_VIEW_FONT_SIZE = 10;
    private const int LIST_VIEW_FONT_SIZE = 12;

    #endregion

    #region 缓存管理

    private static readonly Dictionary<string, long> _folderSizeCache = new Dictionary<string, long>();
    private static readonly HashSet<string> _calculatingFolders = new HashSet<string>();
    private static float _lastCacheCleanupTime;

    #endregion

    #region 初始化

    static FolderSizeEditor()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        EditorApplication.update += UpdateCacheCleanup;
    }

    #endregion

    #region 主要逻辑

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (!IsValidAsset(assetPath)) return;

        var sizeInfo = GetAssetSizeInfo(assetPath);
        if (sizeInfo == null) return;

        DrawSizeLabel(selectionRect, sizeInfo);
    }

    private static bool IsValidAsset(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets")) return false;

        var isFolder = AssetDatabase.IsValidFolder(assetPath);
        return isFolder || File.Exists(assetPath);
    }

    private static AssetSizeInfo GetAssetSizeInfo(string assetPath)
    {
        var isFolder = AssetDatabase.IsValidFolder(assetPath);

        if (isFolder)
        {
            return GetFolderSizeInfo(assetPath);
        }

        return GetFileSizeInfo(assetPath);
    }

    #endregion

    #region 文件大小处理

    private static AssetSizeInfo GetFileSizeInfo(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var fileInfo = new FileInfo(filePath);
        return new AssetSizeInfo
        {
            SizeText = FormatFileSize(fileInfo.Length),
            IsFolder = false,
            IsCalculating = false
        };
    }

    #endregion

    #region 文件夹大小处理

    private static AssetSizeInfo GetFolderSizeInfo(string folderPath)
    {
        if (_folderSizeCache.TryGetValue(folderPath, out var cachedSize))
        {
            return CreateFolderSizeInfo(FormatFileSize(cachedSize), false);
        }

        if (_calculatingFolders.Contains(folderPath))
        {
            return CreateFolderSizeInfo("计算中...", true);
        }

        StartAsyncFolderSizeCalculation(folderPath);
        return CreateFolderSizeInfo("计算中...", true);
    }

    private static AssetSizeInfo CreateFolderSizeInfo(string sizeText, bool isCalculating)
    {
        return new AssetSizeInfo
        {
            SizeText = sizeText,
            IsFolder = true,
            IsCalculating = isCalculating
        };
    }

    private static async void StartAsyncFolderSizeCalculation(string folderPath)
    {
        _calculatingFolders.Add(folderPath);

        try
        {
            var size = await CalculateFolderSizeAsync(folderPath);
            CacheFolderSize(folderPath, size);
            EditorApplication.RepaintProjectWindow();
        }
        catch (System.Exception ex)
        {
            LogCalculationError(folderPath, ex);
        }
        finally
        {
            _calculatingFolders.Remove(folderPath);
        }
    }

    private static Task<long> CalculateFolderSizeAsync(string folderPath)
    {
        return Task.Run(() => CalculateFolderSize(folderPath));
    }

    private static long CalculateFolderSize(string folderPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

            long totalSize = 0;
            foreach (var file in files)
            {
                totalSize += file.Length;
            }
            return totalSize;
        }
        catch
        {
            return 0;
        }
    }

    private static void CacheFolderSize(string folderPath, long size)
    {
        _folderSizeCache[folderPath] = size;
    }

    private static void LogCalculationError(string folderPath, System.Exception ex)
    {
        Debug.LogWarning($"计算文件夹大小失败: {folderPath}. 错误: {ex.Message}");
    }

    #endregion

    #region UI绘制

    private static void DrawSizeLabel(Rect selectionRect, AssetSizeInfo sizeInfo)
    {
        var isGridView = IsGridView(selectionRect);

        if (isGridView)
        {
            DrawGridViewSizeLabel(selectionRect, sizeInfo);
        }
        else
        {
            DrawListViewSizeLabel(selectionRect, sizeInfo);
        }
    }

    private static bool IsGridView(Rect selectionRect)
    {
        return selectionRect.height > GRID_VIEW_HEIGHT_THRESHOLD;
    }

    private static void DrawGridViewSizeLabel(Rect selectionRect, AssetSizeInfo sizeInfo)
    {
        var style = CreateGridViewStyle(sizeInfo);
        var shadowStyle = CreateShadowStyle(style);

        var labelRect = CalculateGridViewLabelRect(selectionRect, sizeInfo.SizeText, style);
        var shadowRect = CalculateShadowRect(labelRect);

        GUI.Label(shadowRect, sizeInfo.SizeText, shadowStyle);
        GUI.Label(labelRect, sizeInfo.SizeText, style);
    }

    private static void DrawListViewSizeLabel(Rect selectionRect, AssetSizeInfo sizeInfo)
    {
        var style = CreateListViewStyle(sizeInfo);
        var labelRect = CalculateListViewLabelRect(selectionRect, sizeInfo.SizeText, style);

        GUI.Label(labelRect, sizeInfo.SizeText, style);
    }

    #endregion

    #region 样式创建

    private static GUIStyle CreateGridViewStyle(AssetSizeInfo sizeInfo)
    {
        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperRight,
            fontSize = GRID_VIEW_FONT_SIZE,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = GetTextColor(sizeInfo);
        return style;
    }

    private static GUIStyle CreateListViewStyle(AssetSizeInfo sizeInfo)
    {
        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = LIST_VIEW_FONT_SIZE
        };
        style.normal.textColor = GetTextColor(sizeInfo);
        return style;
    }

    private static GUIStyle CreateShadowStyle(GUIStyle originalStyle)
    {
        var shadowStyle = new GUIStyle(originalStyle);
        shadowStyle.normal.textColor = Color.black;
        return shadowStyle;
    }

    private static Color GetTextColor(AssetSizeInfo sizeInfo)
    {
        if (sizeInfo.IsCalculating) return Color.gray;
        return !sizeInfo.IsFolder ? Color.yellow : Color.white;
    }

    #endregion

    #region 布局计算

    private static Rect CalculateGridViewLabelRect(Rect selectionRect, string text, GUIStyle style)
    {
        var textSize = style.CalcSize(new GUIContent(text));
        return new Rect(
            selectionRect.xMax - textSize.x - 2,
            selectionRect.y + 2,
            textSize.x,
            textSize.y
        );
    }

    private static Rect CalculateListViewLabelRect(Rect selectionRect, string text, GUIStyle style)
    {
        var textSize = style.CalcSize(new GUIContent(text));
        return new Rect(
            selectionRect.xMax - textSize.x - 5,
            selectionRect.y,
            textSize.x,
            selectionRect.height
        );
    }

    private static Rect CalculateShadowRect(Rect labelRect)
    {
        return new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width, labelRect.height);
    }

    #endregion

    #region 缓存管理

    private static void UpdateCacheCleanup()
    {
        if (Time.realtimeSinceStartup - _lastCacheCleanupTime > CACHE_CLEANUP_INTERVAL)
        {
            CleanupCache();
            _lastCacheCleanupTime = Time.realtimeSinceStartup;
        }
    }

    private static void CleanupCache()
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in _folderSizeCache)
        {
            if (!Directory.Exists(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _folderSizeCache.Remove(key);
            _calculatingFolders.Remove(key);
        }
    }

    #endregion

    #region 工具方法

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";

        var sizes = new[] { "B", "KB", "MB", "GB", "TB" };
        var len = (double)bytes;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    #endregion

    #region 数据结构

    private class AssetSizeInfo
    {
        public string SizeText { get; set; }
        public bool IsFolder { get; set; }
        public bool IsCalculating { get; set; }
    }

    #endregion
}