using System;
using System.Collections.Generic;

namespace TEngine
{
    /// <summary>
    /// 游戏事件参数基类。
    /// </summary>
    public abstract class GameEventArgs : IMemory
    {
        /// <summary>
        /// 获取指定事件类型的ID。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <returns>事件ID。</returns>
        public static int GetEventId<T>() where T : GameEventArgs
        {
            return RuntimeId.ToRuntimeId(typeof(T).Name);
        }

        /// <summary>
        /// 清理内存对象回收入池。
        /// </summary>
        public abstract void Clear();
    }
}