using System;
using System.Collections.Generic;

namespace PFGraph
{
    public enum NotifyType
    {
        Added,
        Changed,
        Remove
    }

    public struct BlackBoardEventArg
    {
        public object Value;
        public NotifyType NotifyType;
    }

    public class BlackboardProcessor<TKey> : IBlackboard<TKey>
    {
        private readonly Blackboard<TKey> blackboard;
        private readonly EventStation<TKey> events;
        private readonly List<KeyValuePair<TKey, Action<BlackBoardEventArg>>> addObservers;
        private readonly List<KeyValuePair<TKey, Action<BlackBoardEventArg>>> removeObservers;
        private bool isNotifying;

        public Blackboard<TKey> Blackboard => blackboard;

        public EventStation<TKey> Events => events;

        public BlackboardProcessor(Blackboard<TKey> blackboard) : this(blackboard, new EventStation<TKey>()) { }

        public BlackboardProcessor(Blackboard<TKey> blackboard, EventStation<TKey> events)
        {
            this.blackboard = blackboard;
            this.events = events;
            this.addObservers = new List<KeyValuePair<TKey, Action<BlackBoardEventArg>>>();
            this.removeObservers = new List<KeyValuePair<TKey, Action<BlackBoardEventArg>>>();
        }

        public bool Contains(TKey key)
        {
            return blackboard.Contains(key);
        }

        public T Get<T>(TKey key)
        {
            return blackboard.Get<T>(key);
        }

        public object Get(TKey key)
        {
            return blackboard.Get(key);
        }

        public bool TryGet<T>(TKey key, out T value)
        {
            return blackboard.TryGet(key, out value);
        }

        public bool TryGet(TKey key, out object value)
        {
            return blackboard.TryGet(key, out value);
        }

        public bool Set<T>(TKey key, T value)
        {
            var notifyType = NotifyType.Changed;
            if (!blackboard.Contains(key))
            {
                notifyType = NotifyType.Added;
            }

            if (blackboard.Set(key, value))
            {
                NotifyObservers(key, value, notifyType);
                return true;
            }

            return false;
        }

        public bool Remove(TKey key)
        {
            if (!blackboard.TryGet(key, out var value))
            {
                return false;
            }

            blackboard.Remove(key);
            NotifyObservers(key, value, NotifyType.Remove);
            return true;
        }

        public void Clear()
        {
            blackboard.Clear();
            events.UnregisterAll();
            addObservers.Clear();
            removeObservers.Clear();
        }

        private void NotifyObservers(TKey key, object value, NotifyType notifyType)
        {
            if (!events.HasEvent(key))
                return;

            addObservers.Clear();
            removeObservers.Clear();

            isNotifying = true;
            try
            {
                events.Publish(key, new BlackBoardEventArg() { Value = value, NotifyType = notifyType });
            }
            finally
            {
                isNotifying = false;
            }

            foreach (var pair in removeObservers)
            {
                UnregisterObserver(pair.Key, pair.Value);
            }

            foreach (var pair in addObservers)
            {
                RegisterObserver(pair.Key, pair.Value);
            }

            addObservers.Clear();
            removeObservers.Clear();
        }

        public void RegisterObserver(TKey key, Action<BlackBoardEventArg> observer)
        {
            if (isNotifying)
            {
                addObservers.Add(new KeyValuePair<TKey, Action<BlackBoardEventArg>>(key, observer));
                return;
            }

            events.Subscribe(key, observer);
        }

        public void UnregisterObserver(TKey key, Action<BlackBoardEventArg> observer)
        {
            if (isNotifying)
            {
                removeObservers.Add(new KeyValuePair<TKey, Action<BlackBoardEventArg>>(key, observer));
                return;
            }

            events.Unsubscribe(key, observer);
        }
    }
}