using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PFPackage.FeiShuExcel
{
    public static partial class FeiShuUtils
    {
        public const string URL_GET_FOLDER_CHECK_LIST = "https://open.feishu.cn/open-apis/drive/v1/files";
        public const string URL_CREATE_FOLDER = "https://open.feishu.cn/open-apis/drive/v1/files/create_folder";
        public const string URL_CREATE_EXCEL = "https://open.feishu.cn/open-apis/sheets/v3/spreadsheets";
        public const string URL_WRITE_SHEET = "https://open.feishu.cn/open-apis/sheets/v2/spreadsheets/{0}/values";
        public const string URL_GET_SHEET_INFO = "https://open.feishu.cn/open-apis/sheets/v3/spreadsheets/{0}/sheets/query";
        public const string URL_GET_ACCESS_TOKEN = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";
        public const string URL_UPDATE_SHEET = "https://open.feishu.cn/open-apis/sheets/v2/spreadsheets/{0}/sheets_batch_update";
        public const string URL_Merge_Cell = "https://open.feishu.cn/open-apis/sheets/v2/spreadsheets/{0}/merge_cells";
        public const string URL_READ_SHEET = "https://open.feishu.cn/open-apis/sheets/v2/spreadsheets/{0}/values/{1}";
        
        private static readonly SemaphoreSlim writeOnlineSemaphore = new SemaphoreSlim(10);
        private static readonly SemaphoreSlim writeLocalSemaphore = new SemaphoreSlim(10);


        #region 本地同步到远端

        /// <summary>
        /// 递归同步本地目录结构到飞书云端
        /// </summary>
        /// <param name="localPath">本地目录路径</param>
        /// <param name="parentToken">云端父文件夹 token</param>
        /// <returns>
        /// 本地路径到云端 token 的映射字典
        /// Key: 本地文件路径
        /// Value: 云端文件 token
        /// </returns>
        public static async Task<Dictionary<string, string>> SyncLocalFileTreeToOnline(string localPath, string parentToken)
        {
            Dictionary<string, string> pathToTokenMap = new Dictionary<string, string>();

            try
            {
                // 获取云端当前目录的文件列表
                List<FeiShuFileInfo> existingFiles = await GetFolderRootCheckList(parentToken);

                // 云端的所有子文件夹和表格
                Dictionary<string, string> folders = existingFiles.Where(f => f.Type == "folder").ToDictionary(f => f.Name, f => f.Token);
                Dictionary<string, string> excels = existingFiles.Where(f => f.Type == "sheet").ToDictionary(f => f.Name, f => f.Token);

                // 遍历本地目录
                foreach (string item in Directory.GetFileSystemEntries(localPath))
                {
                    string name = Path.GetFileNameWithoutExtension(item);

                    if (name.StartsWith("~")) continue;

                    //处理文件夹
                    if (Directory.Exists(item))
                    {
                        if (!folders.TryGetValue(name, out string existingFolderToken))
                        {
                            existingFolderToken = await CreateFeiShuFolder(name, parentToken);
                        }
                        Dictionary<string, string> childMap = await SyncLocalFileTreeToOnline(item, existingFolderToken);
                        foreach (KeyValuePair<string, string> kvp in childMap)
                        {
                            pathToTokenMap[kvp.Key] = kvp.Value;
                        }
                    }
                    //处理Excel文件
                    else if (File.Exists(item) && IsExcelFile(item))
                    {
                        if (excels.TryGetValue(name, out string existingSheetToken))
                        {
                            pathToTokenMap[item] = existingSheetToken;
                        }
                        else
                        {
                            string newExcelToken = await CreateFeiShuExcel(name, parentToken);
                            pathToTokenMap[item] = newExcelToken;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 同步目录时发生错误: {ex.Message}");
            }

            return pathToTokenMap;
        }

        /// <summary>
        /// 同步本地配置表到远端(递归)
        /// </summary>
        public static async Task SyncLocalExcelToOnline()
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                string localRootPath = FeiShuExcelSetting.I.LocalRootPath;

                if (string.IsNullOrEmpty(localRootPath))
                {
                    Debug.LogError("[飞书同步] 本地根目录路径未配置");
                    return;
                }

                if (!Directory.Exists(localRootPath))
                {
                    Debug.LogError($"[飞书同步] 本地根目录不存在: {localRootPath}");
                    return;
                }

                EditorUtility.DisplayProgressBar("飞书同步", "准备同步中...", 0f);
                Debug.Log($"[飞书同步] 开始同步本地目录: {localRootPath}");
                EditorUtility.DisplayProgressBar("飞书同步", "同步目录结构...", 0.2f);

                var syncResult = await SyncLocalFileTreeToOnline(localRootPath, FeiShuExcelSetting.I.FeiShuFolderRootToken);

                var excelFiles = syncResult.Count(kvp => IsExcelFile(kvp.Key));

                Debug.Log($"[飞书同步] 开始同步 {excelFiles} 个Excel表");
                EditorUtility.DisplayProgressBar("飞书同步", $"同步 {excelFiles} 个Excel文件...", 0.5f);
                
                var excelSyncTasks = syncResult.Select(async pair =>
                {
                    await writeOnlineSemaphore.WaitAsync();
                    try
                    {
                        await WriteOnlineExcel(pair.Value, pair.Key);
                    }
                    finally
                    {
                        writeOnlineSemaphore.Release();
                    }
                }).ToArray();
                await Task.WhenAll(excelSyncTasks);
                stopwatch.Stop();

                EditorUtility.DisplayProgressBar("飞书同步", "同步完成！", 0.9f);
                Debug.Log($"[飞书同步] 同步完成！当前共有 {syncResult.Count} 个文件" +
                          $" 映射信息：\n{string.Join("\n", syncResult.Select(kvp => $"{kvp.Key} -> {kvp.Value}"))}");
                Debug.Log($"[飞书同步] 表格同步完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
                // 清除进度条
                EditorUtility.ClearProgressBar();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar(); // 确保异常时也清除进度条
                Debug.LogError($"[飞书同步] 同步过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region 远端同步到本地

        /// <summary>
        /// 递归同步飞书云端目录结构到本地
        /// </summary>
        /// <param name="localPath">本地目录路径</param>
        /// <param name="parentToken">云端父文件夹 token</param>
        /// <returns>
        /// 本地路径到云端 token 的映射字典
        /// Key: 云端文件 token
        /// Value: 本地文件路径
        /// </returns>
        public static async Task<Dictionary<string, string>> SyncOnlineFileTreeToLocal(string parentToken, string localPath)
        {
            var tokenToPathMap = new Dictionary<string, string>();

            try
            {
                // 获取云端当前目录的文件列表
                List<FeiShuFileInfo> existingFiles = await GetFolderRootCheckList(parentToken);

                foreach (var file in existingFiles)
                {
                    if (file.Name.StartsWith("~")) continue;
                    //文件夹
                    if (file.Type == "folder")
                    {
                        string localFolderPath = Path.Combine(localPath, file.Name);

                        var childMap = await SyncOnlineFileTreeToLocal(file.Token, localFolderPath);
                        foreach (var kvp in childMap)
                        {
                            tokenToPathMap[kvp.Key] = kvp.Value;
                        }
                    }
                    else if(file.Type == "sheet")
                    {
                        string localFilePath = Path.Combine(localPath, file.Name + ".xlsx");

                        tokenToPathMap[file.Token] = localFilePath;

                        if (!File.Exists(localFilePath))
                        {
                            await File.Create(localFilePath).DisposeAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 同步目录时发生错误: {ex.Message}");
            }
            return tokenToPathMap;
        }

        /// <summary>
        /// 同步远端配置表到本地(递归)
        /// </summary>
        public static async Task SyncOnlineExcelToLocal()
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                string localRootPath = FeiShuExcelSetting.I.LocalRootPath;
                
                if (string.IsNullOrEmpty(localRootPath))
                {
                    Debug.LogError("[飞书同步] 本地根目录路径未配置");
                    return;
                }

                if (!Directory.Exists(localRootPath))
                {
                    Debug.LogError($"[飞书同步] 本地根目录不存在: {localRootPath}");
                    return;
                }
                
                EditorUtility.DisplayProgressBar("飞书同步", "准备同步中...", 0f);
                Debug.Log($"[飞书同步] 开始同步本地目录: {localRootPath}");
                EditorUtility.DisplayProgressBar("飞书同步", "同步目录结构...", 0.2f);
                
                var syncResult = await SyncOnlineFileTreeToLocal(FeiShuExcelSetting.I.FeiShuFolderRootToken, localRootPath);
                
                var excelFiles = syncResult.Count(kvp => IsExcelFile(kvp.Value));

                Debug.Log($"[飞书同步] 开始同步 {excelFiles} 个Excel表");
                EditorUtility.DisplayProgressBar("飞书同步", $"同步 {excelFiles} 个Excel文件...", 0.5f);

                var excelSyncTasks = syncResult.Select(async pair =>
                {
                    await writeOnlineSemaphore.WaitAsync();
                    try
                    {
                        await WriteLocalExcel(pair.Key, pair.Value);
                    }
                    finally
                    {
                        writeOnlineSemaphore.Release();
                    }
                }).ToArray();
                await Task.WhenAll(excelSyncTasks);
                stopwatch.Stop();
                EditorUtility.DisplayProgressBar("飞书同步", "同步完成！", 0.9f);
                
                Debug.Log($"[飞书同步] 同步完成！当前共有 {syncResult.Count} 个文件" +
                          $" 映射信息：\n{string.Join("\n", syncResult.Select(kvp => $"{kvp.Key} -> {kvp.Value}"))}");
                Debug.Log($"[飞书同步] 表格同步完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
                // 清除进度条
                EditorUtility.ClearProgressBar();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar(); // 确保异常时也清除进度条
                Debug.LogError($"[飞书同步] 同步过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        #endregion
    }
}