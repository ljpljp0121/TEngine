using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        
          /// <summary>
        /// 递归同步目录结构到飞书云端
        /// </summary>
        /// <param name="localPath">本地目录路径</param>
        /// <param name="parentToken">云端父文件夹 token</param>
        /// <returns>本地路径到云端 token 的映射字典</returns>
        public static async Task<Dictionary<string, string>> SyncDirectoryStructure(string localPath, string parentToken)
        {
            var pathToTokenMap = new Dictionary<string, string>();

            try
            {
                // 获取云端当前目录的文件列表
                var existingFiles = await GetFolderRootCheckList(parentToken);

                // 云端的所有子文件夹和表格
                var folders = existingFiles.Where(f => f.Type == "folder").ToDictionary(f => f.Name, f => f.Token);
                var excels = existingFiles.Where(f => f.Type == "sheet").ToDictionary(f => f.Name, f => f.Token);

                // 遍历本地目录
                foreach (var item in Directory.GetFileSystemEntries(localPath))
                {
                    var name = Path.GetFileNameWithoutExtension(item);

                    if (name.StartsWith("~")) continue;

                    //处理文件夹
                    if (Directory.Exists(item))
                    {
                        if (!folders.TryGetValue(name, out string existingFolderToken))
                        {
                            await CreateFeiShuFolder(name, parentToken);
                        }
                        var childMap = await SyncDirectoryStructure(item, pathToTokenMap[item]);
                        foreach (var kvp in childMap)
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

                Debug.Log($"[飞书同步] 开始同步本地目录: {localRootPath}");
                var syncResult = await SyncDirectoryStructure(localRootPath, FeiShuExcelSetting.I.FeiShuFolderRootToken);
                Debug.Log($"[飞书同步] 文件结构同步完成");
                
                Debug.Log($"[飞书同步] 开始同步所有Excel表");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var excelSyncTasks = syncResult.Select(pair => WriteOnlineExcel(pair.Value, pair.Key)).ToArray();
                await Task.WhenAll(excelSyncTasks);
                stopwatch.Stop();
                Debug.Log($"[飞书同步] 表格同步完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
                
                Debug.Log($"[飞书同步] 同步完成！当前共有 {syncResult.Count} 个文件(包括文件夹)" +
                          $" 映射信息：\n{string.Join("\n", syncResult.Select(kvp => $"{kvp.Key} -> {kvp.Value}"))}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书同步] 同步过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}