using System;
using System.Globalization;
using UnityEngine;

namespace TEngine
{
    public partial class SaveModule
    {
        /// <summary>
        /// 单个存档的数据
        /// </summary>
        [Serializable]
        public class SaveItem
        {
            public int SaveID;
            private DateTime lastSaveTime;
            public DateTime LastSaveTime
            {
                get
                {
                    if (lastSaveTime == default(DateTime))
                    {
                        DateTime.TryParse(LastSaveTimeString, out lastSaveTime);
                    }
                    return lastSaveTime;
                }
            }
            [SerializeField] private string LastSaveTimeString; // Json不支持DateTime，用来持久化的

            public SaveItem(int saveID, DateTime lastSaveTime)
            {
                this.SaveID = saveID;
                this.lastSaveTime = lastSaveTime;
                LastSaveTimeString = lastSaveTime.ToString(CultureInfo.InvariantCulture);
            }

            public void UpdateTime(DateTime lastSaveTime)
            {
                this.lastSaveTime = lastSaveTime;
                LastSaveTimeString = lastSaveTime.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}