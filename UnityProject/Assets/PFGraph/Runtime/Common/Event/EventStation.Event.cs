using System;
using System.Collections.Generic;

namespace PFGraph
{
    public partial class EventStation<TKey>
    {
        public class Event : EventBase, IEvent
        {
            private readonly List<Action> handlers = new(8);
            private readonly Queue<Action> handlerQueue = new(8);

            public void Add(Action handler)
            {
                handlers.Add(handler);
            }

            public void Remove(Action handler)
            {
                handlers.Remove(handler);
            }

            public void Clear()
            {
                handlers.Clear();
            }

            public void Invoke()
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
                        handlerQueue.Dequeue()?.Invoke();
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