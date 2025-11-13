using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TEngine
{
    public partial class SaveModule : Module, ISaveModule
    {
        private static IBinarySerializer binarySerializer;

        #region 存档系统、存档系统数据类及所有用户存档、设置存档数据

        private static SaveModuleData _saveModuleData;

        // 存档的保存
        private const string saveDirName = "saveData";
        // 设置的保存：1.全局数据的保存（分辨率、按键设置） 2.存档的设置保存。
        // 常规情况下，存档系统自行维护
        private const string settingDirName = "setting";

        // 存档文件夹路径
        private static string saveDirPath;
        private static string settingDirPath;

        // 存档中对象的缓存字典
        // <存档ID,<文件名称，实际的对象>>
        private static Dictionary<int, Dictionary<string, object>> cacheDic =
            new Dictionary<int, Dictionary<string, object>>();

        #endregion

        #region 获取、删除所有用户存档

        /// <summary>
        /// 获取所有存档
        /// 按创建时间升序
        /// </summary>
        public List<SaveItem> GetAllSaveItem()
        {
            return _saveModuleData.saveItemList;
        }

        /// <summary>
        /// 获取所有存档
        /// 按创建时间降序
        /// </summary>
        public List<SaveItem> GetAllSaveItemByCreatTime()
        {
            List<SaveItem> saveItems = new List<SaveItem>(_saveModuleData.saveItemList);
            saveItems.Reverse();
            return saveItems;
        }

        /// <summary>
        /// 获取所有存档
        /// 按更新时间降序
        /// </summary>
        public List<SaveItem> GetAllSaveItemByUpdateTime()
        {
            List<SaveItem> saveItems = new List<SaveItem>(_saveModuleData.saveItemList);
            OrderByUpdateTimeComparer orderBy = new OrderByUpdateTimeComparer();
            saveItems.Sort(orderBy);
            return saveItems;
        }

        private class OrderByUpdateTimeComparer : IComparer<SaveItem>
        {
            public int Compare(SaveItem x, SaveItem y)
            {
                return y.LastSaveTime.CompareTo(x.LastSaveTime);
            }
        }

        /// <summary>
        /// 获取所有存档
        /// </summary>
        public List<SaveItem> GetAllSaveItem<T>(Func<SaveItem, T> orderFunc, bool isDescending = false)
        {
            return isDescending
                ? _saveModuleData.saveItemList.OrderByDescending(orderFunc).ToList()
                : _saveModuleData.saveItemList.OrderBy(orderFunc).ToList();
        }

        public void DeleteAllSaveItem()
        {
            if (Directory.Exists(saveDirPath))
            {
                // 直接删除目录
                Directory.Delete(saveDirPath, true);
            }
            CheckAndCreateDir();
            InitSaveSystemData();
        }

        public void DeleteAll()
        {
            ClearCache();
            DeleteAllSaveItem();
            DeleteAllSetting();
        }

        #endregion

        #region 创建、获取、删除某一项用户存档

        /// <summary>
        /// 获取SaveItem
        /// </summary>
        public SaveItem GetSaveItem(int id)
        {
            for (int i = 0; i < _saveModuleData.saveItemList.Count; i++)
            {
                if (_saveModuleData.saveItemList[i].SaveID == id)
                {
                    return _saveModuleData.saveItemList[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取SaveItem
        /// </summary>
        public SaveItem GetSaveItem(SaveItem saveItem)
        {
            return GetSaveItem(saveItem.SaveID);
        }

        /// <summary>
        /// 添加一个存档
        /// </summary>
        /// <returns></returns>
        public SaveItem CreateSaveItem(int saveID = 0)
        {
            SaveItem saveItem = new SaveItem(saveID, DateTime.Now);
            _saveModuleData.saveItemList.Add(saveItem);
            _saveModuleData.saveNum = _saveModuleData.saveItemList.Count;
            // 更新SaveSystemData 写入磁盘
            UpdateSaveSystemData(); 
            return saveItem;
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        /// <param name="saveID">存档的ID</param>
        public void DeleteSaveItem(int saveID)
        {
            try
            {
                // 验证存档是否存在
                SaveItem saveItem = GetSaveItem(saveID);
                if (saveItem == null)
                {
                    Log.Error($"[SaveModule] DeleteSaveItem失败: 存档ID {saveID} 不存在");
                    return;
                }

                string itemDir = GetSavePath(saveID, false);
                // 如果路径存在 且 有效
                if (itemDir != null)
                {
                    try
                    {
                        // 把这个存档下的文件递归删除
                        if (Directory.Exists(itemDir))
                        {
                            Directory.Delete(itemDir, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[SaveModule] 删除存档目录失败: {ex.Message}, path: {itemDir}");
                        // 继续执行，即使目录删除失败也要清理数据结构
                    }
                }

                // 从列表中移除（现在saveItem已经验证不为null）
                _saveModuleData.saveItemList.Remove(saveItem);
                _saveModuleData.saveNum = _saveModuleData.saveItemList.Count;
                // 移除缓存
                RemoveCache(saveID);
                // 更新SaveSystemData 写入磁盘
                UpdateSaveSystemData();
            }
            catch (Exception ex)
            {
                Log.Error($"[SaveModule] DeleteSaveItem异常: {ex.Message}, saveID={saveID}");
            }
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteSaveItem(SaveItem saveItem)
        {
            DeleteSaveItem(saveItem.SaveID);
        }

        #endregion

        #region 更新、获取、删除用户存档缓存

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="saveID">存档ID</param>
        /// <param name="fileName">文件名称</param>
        /// <param name="saveObject">要缓存的对象</param>
        private void SetCache(int saveID, string fileName, object saveObject)
        {
            if (cacheDic.ContainsKey(saveID))
            {
                if (cacheDic[saveID].ContainsKey(fileName))
                {
                    cacheDic[saveID][fileName] = saveObject;
                }
                else
                {
                    cacheDic[saveID].Add(fileName, saveObject);
                }
            }
            else
            {
                cacheDic.Add(saveID, new Dictionary<string, object>() { { fileName, saveObject } });
            }
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="saveID">存档ID</param>
        /// <param name="fileName">文件名</param>
        private T GetCache<T>(int saveID, string fileName) where T : class
        {
            // 缓存字典中是否有这个SaveID
            if (cacheDic.ContainsKey(saveID))
            {
                // 这个存档中有没有这个文件
                if (cacheDic[saveID].ContainsKey(fileName))
                {
                    return cacheDic[saveID][fileName] as T;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        private void RemoveCache(int saveID)
        {
            cacheDic.Remove(saveID);
        }

        /// <summary>
        /// 移除缓存中的某一个对象
        /// </summary>
        private void RemoveCache(int saveID, string fileName)
        {
            cacheDic[saveID].Remove(fileName);
        }

        public void ClearCache()
        {
            cacheDic.Clear();
        }

        #endregion

        #region 保存、获取、删除用户存档中某一对象

        /// <summary>
        /// 保存对象至某个存档中
        /// </summary>
        /// <param name="saveObject">要保存的对象</param>
        /// <param name="saveFileName">保存的文件名称</param>
        /// <param name="saveID">存档的ID</param>
        public void SaveObject(object saveObject, string saveFileName, int saveID = 0)
        {
            // 验证输入参数
            if (saveObject == null)
            {
                Log.Error($"[SaveModule] SaveObject失败: saveObject为null, saveID={saveID}, fileName={saveFileName}");
                return;
            }

            if (string.IsNullOrEmpty(saveFileName))
            {
                Log.Error($"[SaveModule] SaveObject失败: saveFileName为空, saveID={saveID}");
                return;
            }

            // 验证存档是否存在 - 这里是关键的空引用防护
            SaveItem saveItem = GetSaveItem(saveID);
            if (saveItem == null)
            {
                Log.Error($"[SaveModule] SaveObject失败: 存档ID {saveID} 不存在");
                return;
            }

            try
            {
                // 存档所在的文件夹路径
                string dirPath = GetSavePath(saveID, true);
                // 具体的对象要保存的路径
                string savePath = dirPath + "/" + saveFileName;
                // 具体的保存
                SaveFile(saveObject, savePath);
                // 更新存档时间 - 现在安全了，saveItem已经验证不为null
                saveItem.UpdateTime(DateTime.Now);
                // 更新SaveSystemData 写入磁盘
                UpdateSaveSystemData();

                // 更新缓存
                SetCache(saveID, saveFileName, saveObject);
            }
            catch (Exception ex)
            {
                Log.Error($"[SaveModule] SaveObject异常: {ex.Message}, saveID={saveID}, fileName={saveFileName}");
            }
        }

        public void SaveObject(object saveObject, string saveFileName, SaveItem saveItem)
        {
            if (saveItem == null)
            {
                Log.Error("[SaveModule] SaveObject失败: saveItem为null");
                return;
            }
            SaveObject(saveObject, saveFileName, saveItem.SaveID);
        }

        public void SaveObject(object saveObject, int saveID = 0)
        {
            if (saveObject == null)
            {
                Log.Error("[SaveModule] SaveObject失败: saveObject为null");
                return;
            }
            SaveObject(saveObject, saveObject.GetType().Name, saveID);
        }

        public void SaveObject(object saveObject, SaveItem saveItem)
        {
            if (saveObject == null)
            {
                Log.Error("[SaveModule] SaveObject失败: saveObject为null");
                return;
            }
            if (saveItem == null)
            {
                Log.Error("[SaveModule] SaveObject失败: saveItem为null");
                return;
            }
            SaveObject(saveObject, saveObject.GetType().Name, saveItem);
        }

        /// <summary>
        /// 从某个具体的存档中加载某个对象
        /// </summary>
        /// <typeparam name="T">要返回的实际类型</typeparam>
        /// <param name="saveFileName">文件名称</param>
        /// <param name="saveID">存档ID</param>
        public T LoadObject<T>(string saveFileName, int saveID = 0) where T : class
        {
            T obj = GetCache<T>(saveID, saveFileName);
            if (obj == null)
            {
                // 存档所在的文件夹路径
                string dirPath = GetSavePath(saveID);
                if (dirPath == null) return null;
                // 具体的对象要保存的路径
                string savePath = dirPath + "/" + saveFileName;
                obj = LoadFile<T>(savePath);
                SetCache(saveID, saveFileName, obj);
            }
            return obj;
        }

        public T LoadObject<T>(string saveFileName, SaveItem saveItem) where T : class
        {
            return LoadObject<T>(saveFileName, saveItem.SaveID);
        }

        public T LoadObject<T>(int saveID = 0) where T : class
        {
            return LoadObject<T>(typeof(T).Name, saveID);
        }

        public T LoadObject<T>(SaveItem saveItem) where T : class
        {
            if (saveItem == null)
            {
                Log.Error("[SaveModule] LoadObject失败: saveItem为null");
                return null;
            }
            return LoadObject<T>(typeof(T).Name, saveItem.SaveID);
        }

        /// <summary>
        /// 删除某个存档中的某个对象
        /// </summary>
        /// <param name="saveFileName">文件名称</param>
        /// <param name="saveID">存档的ID</param>
        public void DeleteObject<T>(string saveFileName, int saveID) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(saveFileName))
                {
                    Log.Error($"[SaveModule] DeleteObject失败: saveFileName为空, saveID={saveID}");
                    return;
                }

                // 验证存档是否存在
                if (GetSaveItem(saveID) == null)
                {
                    Log.Error($"[SaveModule] DeleteObject失败: 存档ID {saveID} 不存在");
                    return;
                }

                //清空缓存中对象
                if (GetCache<T>(saveID, saveFileName) != null)
                {
                    RemoveCache(saveID, saveFileName);
                }

                // 存档对象所在的文件路径
                string dirPath = GetSavePath(saveID);
                if (dirPath == null)
                {
                    Log.Error($"[SaveModule] DeleteObject失败: 无法获取存档路径, saveID={saveID}");
                    return;
                }

                string savePath = dirPath + "/" + saveFileName;
                
                // 安全删除文件
                if (!SafeFileOperations.DeleteFileSafe(savePath))
                {
                    Log.Warning($"[SaveModule] DeleteObject: 文件删除失败, path={savePath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SaveModule] DeleteObject异常: {ex.Message}, saveID={saveID}, fileName={saveFileName}");
            }
        }

        public void DeleteObject<T>(string saveFileName, SaveItem saveItem) where T : class
        {
            if (saveItem == null)
            {
                Log.Error("[SaveModule] DeleteObject失败: saveItem为null");
                return;
            }
            DeleteObject<T>(saveFileName, saveItem.SaveID);
        }

        public void DeleteObject<T>(int saveID) where T : class
        {
            DeleteObject<T>(typeof(T).Name, saveID);
        }

        public void DeleteObject<T>(SaveItem saveItem) where T : class
        {
            if (saveItem == null)
            {
                Log.Error("[SaveModule] DeleteObject失败: saveItem为null");
                return;
            }
            DeleteObject<T>(typeof(T).Name, saveItem.SaveID);
        }

        #endregion

        #region 保存、获取全局设置存档

        /// <summary>
        /// 加载设置，全局生效，不关乎任何一个存档
        /// </summary>
        public T LoadSetting<T>(string fileName) where T : class
        {
            return LoadFile<T>(settingDirPath + "/" + fileName);
        }

        public T LoadSetting<T>() where T : class
        {
            return LoadSetting<T>(typeof(T).Name);
        }

        /// <summary>
        /// 保存设置，全局生效，不关乎任何一个存档
        /// </summary>
        public void SaveSetting(object saveObject, string fileName)
        {
            SaveFile(saveObject, settingDirPath + "/" + fileName);
        }

        public void SaveSetting(object saveObject)
        {
            SaveSetting(saveObject, saveObject.GetType().Name);
        }

        public void DeleteAllSetting()
        {
            if (Directory.Exists(settingDirPath))
            {
                Directory.Delete(settingDirPath, true);
            }
            CheckAndCreateDir();
        }

        #endregion

        #region 内部工具函数

        /// <summary>
        /// 获取存档系统数据
        /// </summary>
        /// <returns></returns>
        private void InitSaveSystemData()
        {
            _saveModuleData = LoadFile<SaveModuleData>(saveDirPath + "/SaveSystemData");
            if (_saveModuleData == null)
            {
                _saveModuleData = new SaveModuleData();
                UpdateSaveSystemData();
            }
        }

        /// <summary>
        /// 更新存档系统数据
        /// </summary>
        private void UpdateSaveSystemData()
        {
            SaveFile(_saveModuleData, saveDirPath + "/SaveSystemData");
        }

        /// <summary>
        /// 检查路径并创建目录
        /// </summary>
        private void CheckAndCreateDir()
        {
            Log.Info("本地存档路径：" + saveDirPath);
            // 确保路径的存在
            if (Directory.Exists(saveDirPath) == false)
            {
                Directory.CreateDirectory(saveDirPath);
            }
            if (Directory.Exists(settingDirPath) == false)
            {
                Directory.CreateDirectory(settingDirPath);
            }
        }

        /// <summary>
        /// 获取某个存档的路径
        /// </summary>
        /// <param name="saveID">存档ID</param>
        /// <param name="createDir">如果不存在这个路径，是否需要创建</param>
        /// <returns></returns>
        private string GetSavePath(int saveID, bool createDir = true)
        {
            // 验证是否有某个存档
            if (GetSaveItem(saveID) == null) throw new Exception("SaveID 存档不存在！");

            string saveDir = saveDirPath + "/" + saveID;
            // 确定文件夹是否存在
            if (Directory.Exists(saveDir) == false)
            {
                if (createDir)
                {
                    Directory.CreateDirectory(saveDir);
                }
                else
                {
                    return null;
                }
            }

            return saveDir;
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="saveObject">保存的对象</param>
        /// <param name="path">保存的路径</param>
        private void SaveFile(object saveObject, string path)
        {
            switch (Settings.SaveSetting.saveModuleType)
            {
                case SaveModuleType.Binary:
                    if (binarySerializer == null || saveObject.GetType() == typeof(SaveModuleData))
                        IOTool.SaveFile(saveObject, path);
                    else
                    {
                        byte[] bytes = binarySerializer.Serialize(saveObject);
                        File.WriteAllBytes(path, bytes);
                    }
                    break;
                case SaveModuleType.Json:
                    IOTool.SaveJson(saveObject, path);
                    break;
            }
        }

        /// <summary>
        /// 加载文件
        /// </summary>
        /// <typeparam name="T">加载后要转为的类型</typeparam>
        /// <param name="path">加载路径</param>
        private T LoadFile<T>(string path) where T : class
        {
            switch (Settings.SaveSetting.saveModuleType)
            {
                case SaveModuleType.Binary:
                    if (binarySerializer == null || typeof(T) == typeof(SaveModuleData))
                        return IOTool.LoadFile<T>(path);
                    else
                    {
                        if (!File.Exists(path))
                        {
                            return null;
                        }
                        FileStream file = new FileStream(path, FileMode.Open);
                        byte[] bytes = new byte[file.Length];
                        file.Read(bytes, 0, bytes.Length);
                        file.Close();
                        return binarySerializer.Deserialize<T>(bytes);
                    }
                case SaveModuleType.Json:
                    return IOTool.LoadJson<T>(path);
            }
            return null;
        }

        #endregion

        public override void OnInit()
        {
            // binarySerializer =
            saveDirPath = Application.persistentDataPath + "/" + saveDirName;
            settingDirPath = Application.persistentDataPath + "/" + settingDirName;
#if UNITY_EDITOR
            //编辑器与正式包不同目录方便测试
            saveDirPath += "/debug";
            settingDirPath += "/debug";
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif
            CheckAndCreateDir();
            InitSaveSystemData();
            // 避免Editor环境下使用了上一次运行的缓存
            ClearCache();
        }

        public override void Shutdown()
        {
            saveDirPath = "";
            settingDirPath = "";
            ClearCache();
        }
    }
}