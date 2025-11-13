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
            private DateTime createDateTime;
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
            public DateTime CreateDateTime
            {
                get
                {
                    if (createDateTime == default(DateTime))
                    {
                        DateTime.TryParse(CreateDateTimeString, out createDateTime);
                    }
                    return createDateTime;
                }
            }

            [SerializeField]
            private string LastSaveTimeString; // Json不支持DateTime，用来持久化的
            [SerializeField]
            private string CreateDateTimeString;

            public SaveItem(int saveID, DateTime createTime)
            {
                this.SaveID = saveID;
                this.lastSaveTime = createTime;
                this.createDateTime = createTime;
                LastSaveTimeString = createTime.ToString(CultureInfo.InvariantCulture);
                CreateDateTimeString = createTime.ToString(CultureInfo.InvariantCulture);
            }

            public void UpdateTime(DateTime lastSaveTime)
            {
                this.lastSaveTime = lastSaveTime;
                LastSaveTimeString = lastSaveTime.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}