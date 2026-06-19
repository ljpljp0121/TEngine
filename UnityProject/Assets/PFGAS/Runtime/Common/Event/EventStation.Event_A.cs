using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 按键值管理无参和带参事件订阅与派发的事件站。
    /// </summary>
    public partial class EventStation<TKey>
    {
        /// <summary>
        /// 保存并派发单参数处理器的事件。
        /// </summary>
        public class Event<TArg> : EventBase, IEvent<TArg>
        {
            private readonly List<Action<TArg>> handlers = new(8);
            private readonly Queue<Action<TArg>> handlerQueue = new(8);

            public void Add(Action<TArg> handler)
            {
                handlers.Add(handler);
            }

            public void Remove(Action<TArg> handler)
            {
                handlers.Remove(handler);
            }

            public void Clear()
            {
                handlers.Clear();
            }

            public void Invoke(in TArg arg)
            {
                handlerQueue.Clear();
                for (int i = 0; i < handlers.Count; i++)
                {
                    handlerQueue.Enqueue(handlers[i]);
                }

                try
                {
                    while (handlerQueue.Count > 0)
                    {
                        handlerQueue.Dequeue()?.Invoke(arg);
                    }
                }
                finally
                {
                    handlerQueue.Clear();
                }
            }
        }
    }
}
