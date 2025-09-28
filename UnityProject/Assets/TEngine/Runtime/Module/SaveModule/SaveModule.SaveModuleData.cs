using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace TEngine
{
    public partial class SaveModule
    {
        /// <summary>
        /// 存档系统数据类
        /// </summary>
        [Serializable]
        private class SaveModuleData
        {
            // 当前的存档ID
            public int saveNum = 0;
            // 所有存档的列表
            public List<SaveItem> saveItemList = new List<SaveItem>();
        }
    }
}