using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    public List<TKey> Keys;
    public List<TValue> Values;

    private Dictionary<TKey, TValue> _dictionary;

    public SerializableDictionary()
    {
        Keys = new List<TKey>();
        Values = new List<TValue>();
        _dictionary = new Dictionary<TKey, TValue>();
    }

    public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        Keys = new List<TKey>();
        Values = new List<TValue>();
        _dictionary = new Dictionary<TKey, TValue>();

        foreach (var kvp in dictionary)
        {
            Add(kvp.Key, kvp.Value);
        }
    }


    public void OnBeforeSerialize()
    {
        if (_dictionary != null)
        {
            Keys.Clear();
            Values.Clear();
            foreach (var kvp in _dictionary)
            {
                Keys.Add(kvp.Key);
                Values.Add(kvp.Value);
            }
        }
    }

    public void OnAfterDeserialize()
    {
        _dictionary = new Dictionary<TKey, TValue>();

        if (Keys != null && Values != null)
        {
            int count = Math.Min(Keys.Count, Values.Count);
            for (int i = 0; i < count; i++)
            {
                if (Keys[i] != null)
                {
                    _dictionary[Keys[i]] = Values[i];
                }
            }
        }
    }

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            bool isNewKey = !_dictionary.ContainsKey(key);
            _dictionary[key] = value;

            if (isNewKey)
            {
                Keys.Add(key);
                Values.Add(value);
            }
            else
            {
                int index = Keys.IndexOf(key);
                if (index >= 0)
                {
                    Values[index] = value;
                }
            }
        }
    }

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dictionary.Keys;
    ICollection<TValue> IDictionary<TKey, TValue>.Values => _dictionary.Values;
    public int Count => _dictionary.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);
        Keys.Add(key);
        Values.Add(value);
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _dictionary.Contains(item);
    }

    public bool Remove(TKey key)
    {
        if (_dictionary.Remove(key))
        {
            int index = Keys.IndexOf(key);
            if (index >= 0)
            {
                Keys.RemoveAt(index);
                Values.RemoveAt(index);
            }
            return true;
        }
        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    public void Clear()
    {
        _dictionary.Clear();
        Keys.Clear();
        Values.Clear();
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IReadOnlyDictionary<TKey, TValue> AsReadOnly()
    {
        return _dictionary;
    }
}