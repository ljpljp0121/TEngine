using System;
using System.Collections.Generic;

namespace PFGraph
{
    public partial class BindableDictionary<TKey, TValue>
    {
        private readonly SimpleMonitor monitor = new SimpleMonitor();
        private readonly IBridgedValue<Dictionary<TKey, TValue>> bridgedDictionary;

        [field: NonSerialized] public event Action<BindableDictionaryChanged> DictionaryChanged;
        [field: NonSerialized] public event Action CountChanged;
        [field: NonSerialized] public event Action ItemsChanged;

        public ICollection<TKey> Keys => Value.Keys;

        public ICollection<TValue> Values => Value.Values;

        public int Count => Value.Count;

        public bool IsReadOnly => false;

        public TValue this[TKey key]
        {
            get => Value[key];
            set { Set(key, value); }
        }

        public BindableDictionary() : this(new Dictionary<TKey, TValue>()) { }

        public BindableDictionary(Dictionary<TKey, TValue> dictionary)
        {
            this.bridgedDictionary = new BridgedValue<Dictionary<TKey, TValue>>(dictionary);
            this.m_BindableProperty = new BindableProperty<Dictionary<TKey, TValue>>(this.bridgedDictionary);
        }

        public BindableDictionary(Func<Dictionary<TKey, TValue>> getter, Action<Dictionary<TKey, TValue>> setter)
        {
            this.bridgedDictionary = new BridgedValueGetterSetter<Dictionary<TKey, TValue>>(getter, setter);
            this.m_BindableProperty = new BindableProperty<Dictionary<TKey, TValue>>(this.bridgedDictionary);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Value.GetEnumerator();

        public void Clear()
        {
            this.CheckReentrancy();
            this.Value.Clear();
            this.CountChanged?.Invoke();
            this.ItemsChanged?.Invoke();
            this.OnDictionaryReset();
        }

        public void Add(TKey key, TValue value)
        {
            this.CheckReentrancy();
            this.Value.Add(key, value);
            this.CountChanged?.Invoke();
            this.ItemsChanged?.Invoke();
            this.OnDictionaryAddItem(key, value);
        }

        public bool Remove(TKey key)
        {
            this.CheckReentrancy();
            if (!Value.TryGetValue(key, out var value))
                return false;
            var result = Value.Remove(key);
            if (result)
            {
                this.CountChanged?.Invoke();
                this.ItemsChanged?.Invoke();
                this.OnDictionaryRemoveItem(key, value);
            }

            return result;
        }

        public void Set(TKey key, TValue value)
        {
            this.CheckReentrancy();
            if (this.Value.TryGetValue(key, out var oldValue))
            {
                this.Value[key] = value;
                this.ItemsChanged?.Invoke();
                this.OnDictionarySetItem(key, oldValue, value);
            }
            else
            {
                this.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key) => Value.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => Value.TryGetValue(key, out value);

        private IDisposable BlockReentrancy()
        {
            this.monitor.Enter();
            return this.monitor;
        }

        private void CheckReentrancy()
        {
            if (this.monitor.Busy && this.DictionaryChanged != null &&
                this.DictionaryChanged.GetInvocationList().Length > 1)
                throw new InvalidOperationException("Observable Collection Reentrancy Not Allowed");
        }

        private void OnDictionaryReset()
        {
            this.OnCollectionChanged(new BindableDictionaryChanged() { action = BindableDictionaryAction.Reset });
        }

        private void OnDictionaryAddItem(TKey key, TValue value)
        {
            this.OnCollectionChanged(new BindableDictionaryChanged()
                { action = BindableDictionaryAction.Add, addedKey = key, addedValue = value });
        }

        private void OnDictionaryRemoveItem(TKey key, TValue value)
        {
            this.OnCollectionChanged(new BindableDictionaryChanged()
                { action = BindableDictionaryAction.Remove, removedKey = key, removedValue = value });
        }

        private void OnDictionarySetItem(TKey key, TValue oldValue, TValue newValue)
        {
            this.OnCollectionChanged(new BindableDictionaryChanged()
            {
                action = BindableDictionaryAction.Remove, changedKey = key, changedoldValue = oldValue,
                changedNewValue = newValue
            });
        }

        protected virtual void OnCollectionChanged(BindableDictionaryChanged e)
        {
            if (this.DictionaryChanged == null)
                return;
            using (this.BlockReentrancy())
                this.DictionaryChanged(e);
        }

        public struct BindableDictionaryChanged
        {
            public BindableDictionaryAction action;

            public TKey removedKey;
            public TValue removedValue;

            public TKey addedKey;
            public TValue addedValue;

            public TKey changedKey;
            public TValue changedoldValue;
            public TValue changedNewValue;
        }

        [Serializable]
        private class SimpleMonitor : IDisposable
        {
            private int _busyCount;

            public void Enter() => ++this._busyCount;

            public void Dispose() => --this._busyCount;

            public bool Busy => this._busyCount > 0;
        }
    }

    public partial class BindableDictionary<TKey, TValue> : IBindableProperty,
        IBindableProperty<Dictionary<TKey, TValue>>
    {
        private BindableProperty<Dictionary<TKey, TValue>> m_BindableProperty;

        public event Action<object, object> BoxedValueChanged
        {
            add { this.m_BindableProperty.BoxedValueChanged += value; }
            remove { this.m_BindableProperty.BoxedValueChanged -= value; }
        }

        public event Action<Dictionary<TKey, TValue>, Dictionary<TKey, TValue>> ValueChanged
        {
            add { this.m_BindableProperty.ValueChanged += value; }
            remove { this.m_BindableProperty.ValueChanged -= value; }
        }

        public object BoxedValue
        {
            get => m_BindableProperty.BoxedValue;
            set => m_BindableProperty.BoxedValue = value;
        }

        public Dictionary<TKey, TValue> Value
        {
            get => m_BindableProperty.Value;
            set => m_BindableProperty.Value = value;
        }

        public Type ValueType
        {
            get { return m_BindableProperty.ValueType; }
        }

        public bool SetValue(Dictionary<TKey, TValue> value)
        {
            this.CheckReentrancy();
            this.Value = value;
            this.CountChanged?.Invoke();
            this.ItemsChanged?.Invoke();
            this.OnDictionaryReset();
            return true;
        }

        public void SetValueWithoutNotify(Dictionary<TKey, TValue> value)
        {
            this.CheckReentrancy();
            this.m_BindableProperty.SetValueWithoutNotify(value);
        }

        public void ClearValueChangedEvent()
        {
            this.m_BindableProperty.ClearValueChangedEvent();
        }

        public void NotifyValueChanged()
        {
            this.m_BindableProperty.NotifyValueChanged();
        }

        public bool SetValue(object value)
        {
            return this.m_BindableProperty.SetValue(value);
        }

        public void SetValueWithoutNotify(object value)
        {
            this.m_BindableProperty.SetValue(value);
        }

        public void RegisterValueChangedEvent(Action<Dictionary<TKey, TValue>, Dictionary<TKey, TValue>> onValueChanged)
        {
            this.m_BindableProperty.RegisterValueChangedEvent(onValueChanged);
        }

        public void UnregisterValueChangedEvent(
            Action<Dictionary<TKey, TValue>, Dictionary<TKey, TValue>> onValueChanged)
        {
            this.m_BindableProperty.UnregisterValueChangedEvent(onValueChanged);
        }
    }

    public enum BindableDictionaryAction
    {
        Reset,
        Remove,
        Add,
        Set,
    }
}