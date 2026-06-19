using System;
using UnityEngine;

namespace PFDebugger
{
    /// <summary>
    /// 动态扩容环形缓冲区。
    /// 容量不足时自动翻倍，支持 O(1) 头尾删除和批量操作。
    /// </summary>
    public class DynamicCircularBuffer<T>
    {
        private T[] array;
        private int startIndex;

        public int Count { get; private set; }
        public int Capacity => array.Length;

        public T this[int index]
        {
            get => array[(startIndex + index) % array.Length];
            set => array[(startIndex + index) % array.Length] = value;
        }

        public DynamicCircularBuffer(int initialCapacity = 16)
        {
            array = new T[Math.Max(initialCapacity, 2)];
        }

        private void SetCapacity(int capacity)
        {
            T[] newArray = new T[capacity];
            if (Count > 0)
            {
                int elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
                Array.Copy(array, startIndex, newArray, 0, elementsBeforeWrap);
                if (elementsBeforeWrap < Count)
                    Array.Copy(array, 0, newArray, elementsBeforeWrap, Count - elementsBeforeWrap);
            }

            array = newArray;
            startIndex = 0;
        }

        /// <summary>尾部添加，容量不足时自动扩容</summary>
        public void Add(T value)
        {
            if (array.Length == Count)
                SetCapacity(Math.Max(array.Length * 2, 4));
            this[Count++] = value;
        }

        /// <summary>头部插入，容量不足时自动扩容</summary>
        public void AddFirst(T value)
        {
            if (array.Length == Count)
                SetCapacity(Math.Max(array.Length * 2, 4));
            startIndex = (startIndex - 1 + array.Length) % array.Length;
            array[startIndex] = value;
            Count++;
        }

        /// <summary>批量合并另一个 Buffer 的内容</summary>
        public void AddRange(DynamicCircularBuffer<T> other)
        {
            if (other.Count == 0) return;

            if (array.Length < Count + other.Count)
                SetCapacity(Math.Max(array.Length * 2, Count + other.Count));

            int insertStartIndex = (startIndex + Count) % array.Length;
            int elementsBeforeWrap = Mathf.Min(other.Count, array.Length - insertStartIndex);
            int otherElementsBeforeWrap = Mathf.Min(other.Count, other.array.Length - other.startIndex);

            Array.Copy(other.array, other.startIndex, array, insertStartIndex,
                Mathf.Min(elementsBeforeWrap, otherElementsBeforeWrap));

            if (elementsBeforeWrap < otherElementsBeforeWrap)
                Array.Copy(other.array, other.startIndex + elementsBeforeWrap, array, 0,
                    otherElementsBeforeWrap - elementsBeforeWrap);
            else if (elementsBeforeWrap > otherElementsBeforeWrap)
                Array.Copy(other.array, 0, array, insertStartIndex + otherElementsBeforeWrap,
                    elementsBeforeWrap - otherElementsBeforeWrap);

            int copiedElements = Mathf.Max(elementsBeforeWrap, otherElementsBeforeWrap);
            if (copiedElements < other.Count)
                Array.Copy(other.array, copiedElements - otherElementsBeforeWrap,
                    array, copiedElements - elementsBeforeWrap, other.Count - copiedElements);

            Count += other.Count;
        }

        /// <summary>移除并返回头部元素</summary>
        public T RemoveFirst()
        {
            T element = array[startIndex];
            array[startIndex] = default;
            if (++startIndex == array.Length)
                startIndex = 0;
            Count--;
            return element;
        }

        /// <summary>头部裁剪 N 个元素，可选回调</summary>
        public void TrimStart(int trimCount, Action<T> perElementCallback = null)
        {
            TrimInternal(trimCount, startIndex, perElementCallback);
            startIndex = (startIndex + trimCount) % array.Length;
        }

        /// <summary>尾部裁剪 N 个元素，可选回调</summary>
        public void TrimEnd(int trimCount, Action<T> perElementCallback = null)
        {
            TrimInternal(trimCount, (startIndex + Count - trimCount) % array.Length, perElementCallback);
        }

        private void TrimInternal(int trimCount, int trimStartIndex, Action<T> perElementCallback)
        {
            int elementsBeforeWrap = Mathf.Min(trimCount, array.Length - trimStartIndex);
            if (perElementCallback == null)
            {
                Array.Clear(array, trimStartIndex, elementsBeforeWrap);
                if (elementsBeforeWrap < trimCount)
                    Array.Clear(array, 0, trimCount - elementsBeforeWrap);
            }
            else
            {
                for (int i = trimStartIndex, end = trimStartIndex + elementsBeforeWrap; i < end; i++)
                {
                    perElementCallback(array[i]);
                    array[i] = default;
                }

                for (int i = 0, end = trimCount - elementsBeforeWrap; i < end; i++)
                {
                    perElementCallback(array[i]);
                    array[i] = default;
                }
            }

            Count -= trimCount;
        }

        /// <summary>按谓词删除，返回删除数量。可选回调通知元素新索引。</summary>
        public int RemoveAll(Predicate<T> shouldRemove, Action<T, int> onIndexChanged = null)
        {
            int elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
            int removedCount = 0;
            int i = startIndex, newIndex = startIndex, endIndex = startIndex + elementsBeforeWrap;

            for (; i < endIndex; i++)
            {
                if (shouldRemove(array[i]))
                    removedCount++;
                else
                {
                    if (removedCount > 0)
                    {
                        T element = array[i];
                        array[newIndex] = element;
                        onIndexChanged?.Invoke(element, newIndex - startIndex);
                    }

                    newIndex++;
                }
            }

            i = 0;
            endIndex = Count - elementsBeforeWrap;

            if (newIndex >= array.Length)
            {
                newIndex = 0;
                for (; i < endIndex; i++)
                {
                    if (shouldRemove(array[i]))
                        removedCount++;
                    else
                    {
                        if (removedCount > 0)
                        {
                            T element = array[i];
                            array[newIndex] = element;
                            onIndexChanged?.Invoke(element, newIndex + elementsBeforeWrap);
                        }

                        newIndex++;
                    }
                }
            }
            else
            {
                for (; i < endIndex; i++)
                {
                    if (shouldRemove(array[i]))
                        removedCount++;
                    else
                    {
                        if (removedCount > 0)
                        {
                            T element = array[i];
                            array[newIndex] = element;
                            onIndexChanged?.Invoke(element, newIndex - startIndex);
                        }

                        newIndex++;
                    }
                }
            }

            TrimEnd(removedCount);
            return removedCount;
        }

        /// <summary>查找元素索引，未找到返回 -1</summary>
        public int IndexOf(T value)
        {
            int elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
            int index = Array.IndexOf(array, value, startIndex, elementsBeforeWrap);
            if (index >= 0) return index - startIndex;

            if (elementsBeforeWrap < Count)
            {
                index = Array.IndexOf(array, value, 0, Count - elementsBeforeWrap);
                if (index >= 0) return index + elementsBeforeWrap;
            }

            return -1;
        }

        /// <summary>遍历所有元素</summary>
        public void ForEach(Action<T> action)
        {
            int elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
            for (int i = startIndex, end = startIndex + elementsBeforeWrap; i < end; i++)
                action(array[i]);
            for (int i = 0, end = Count - elementsBeforeWrap; i < end; i++)
                action(array[i]);
        }

        /// <summary>清空所有元素</summary>
        public void Clear()
        {
            int elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
            Array.Clear(array, startIndex, elementsBeforeWrap);
            if (elementsBeforeWrap < Count)
                Array.Clear(array, 0, Count - elementsBeforeWrap);
            startIndex = 0;
            Count = 0;
        }
    }
}
