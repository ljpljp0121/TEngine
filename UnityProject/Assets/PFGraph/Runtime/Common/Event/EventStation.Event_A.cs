using System;
using System.Collections.Generic;

namespace PFGraph
{
    public partial class EventStation<TKey>
    {
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

                while (handlerQueue.Count > 0)
                {
                    try
                    {
                        handlerQueue.Dequeue()?.Invoke(arg);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(e);
                    }
                }

                handlerQueue.Clear();
            }
        }
    }
}