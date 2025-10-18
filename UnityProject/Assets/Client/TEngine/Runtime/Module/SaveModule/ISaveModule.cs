using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TEngine
{
    public interface ISaveModule
    {
        /// <summary>
        /// 获取所有存档
        /// 按创建时间升序
        /// </summary>
        public List<SaveModule.SaveItem> GetAllSaveItem();

        /// <summary>
        /// 获取所有存档
        /// 按创建时间降序
        /// </summary>
        public List<SaveModule.SaveItem> GetAllSaveItemByCreatTime();

        /// <summary>
        /// 获取所有存档
        /// 按更新时间降序
        /// </summary>
        public List<SaveModule.SaveItem> GetAllSaveItemByUpdateTime();

        /// <summary>
        /// 获取所有存档
        /// </summary>
        public List<SaveModule.SaveItem> GetAllSaveItem<T>(Func<SaveModule.SaveItem, T> orderFunc, bool isDescending = false);

        public void DeleteAllSaveItem();

        public void DeleteAll();

        /// <summary>
        /// 获取SaveItem
        /// </summary>
        public SaveModule.SaveItem GetSaveItem(int id);

        /// <summary>
        /// 获取SaveItem
        /// </summary>
        public SaveModule.SaveItem GetSaveItem(SaveModule.SaveItem saveItem);

        /// <summary>
        /// 添加一个存档
        /// </summary>
        /// <returns></returns>
        public SaveModule.SaveItem CreateSaveItem(int saveID = 0);

        /// <summary>
        /// 删除存档    
        /// </summary>
        /// <param name="saveID">存档的ID</param>
        public void DeleteSaveItem(int saveID);

        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteSaveItem(SaveModule.SaveItem saveItem);


        public void ClearCache();

        /// <summary>
        /// 保存对象至某个存档中
        /// </summary>
        /// <param name="saveObject">要保存的对象</param>
        /// <param name="saveFileName">保存的文件名称</param>
        /// <param name="saveID">存档的ID</param>
        public void SaveObject(object saveObject, string saveFileName, int saveID = 0);

        public void SaveObject(object saveObject, string saveFileName, SaveModule.SaveItem saveItem);
        public void SaveObject(object saveObject, int saveID = 0);

        public void SaveObject(object saveObject, SaveModule.SaveItem saveItem);
        
        /// 从某个具体的存档中加载某个对象
        /// </summary>
        /// <typeparam name="T">要返回的实际类型</typeparam>
        /// <param name="saveFileName">文件名称</param>
        /// <param name="saveID">存档ID</param>
        public T LoadObject<T>(string saveFileName, int saveID = 0) where T : class;

        public T LoadObject<T>(string saveFileName, SaveModule.SaveItem saveItem) where T : class;

        public T LoadObject<T>(int saveID = 0) where T : class;

        public T LoadObject<T>(SaveModule.SaveItem saveItem) where T : class;

        /// <summary>
        /// 删除某个存档中的某个对象
        /// </summary>
        /// <param name="saveFileName">文件名称</param>
        /// <param name="saveID">存档的ID</param>
        public void DeleteObject<T>(string saveFileName, int saveID) where T : class;

        public void DeleteObject<T>(string saveFileName, SaveModule.SaveItem saveItem) where T : class;

        public void DeleteObject<T>(int saveID) where T : class;

        public void DeleteObject<T>(SaveModule.SaveItem saveItem) where T : class;

        /// <summary>
        /// 加载设置，全局生效，不关乎任何一个存档
        /// </summary>
        public T LoadSetting<T>(string fileName) where T : class;

        public T LoadSetting<T>() where T : class;

        /// <summary>
        /// 保存设置，全局生效，不关乎任何一个存档
        /// </summary>
        public void SaveSetting(object saveObject, string fileName);

        public void SaveSetting(object saveObject);

        public void DeleteAllSetting();
    }
}