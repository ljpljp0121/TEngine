using System;
using System.Collections.Generic;

namespace PFTreeView
{
    public abstract class ObjectPoolBase<T> where T : class
    {
        protected readonly Queue<T> cachedObjects;
        protected int capacity;

        public int Count => cachedObjects.Count;

        public int Capacity
        {
            get => capacity;
            set
            {
                if (value < 0)
                    throw new Exception("Capacity is invalid.");
                if (capacity == value)
                    return;
                capacity = value;
                Release();
            }
        }

        public ObjectPoolBase()
        {
            cachedObjects = new Queue<T>(16);
            capacity = int.MaxValue;
        }

        public ObjectPoolBase(int initialCapacity)
        {
            cachedObjects = new Queue<T>(16);
            if (initialCapacity <= 0)
                throw new ArgumentException("InitialCapacity must be greater than zero.");
            capacity = initialCapacity;
        }

        public T Spawn()
        {
            T obj = cachedObjects.Count > 0 ? cachedObjects.Dequeue() : Create();
            OnSpawn(obj);
            return obj;
        }

        public void Recycle(T obj)
        {
            if (cachedObjects.Count >= capacity)
            {
                OnRelease(obj);
                return;
            }
            cachedObjects.Enqueue(obj);
            OnRecycle(obj);
        }

        public void Release()
        {
            Release(Count - capacity);
        }

        public void Release(int toReleaseCount)
        {
            while (toReleaseCount-- > 0 && cachedObjects.Count > 0)
            {
                OnRelease(cachedObjects.Dequeue());
            }
        }

        public void ReleaseAll()
        {
            while (cachedObjects.Count > 0)
            {
                OnRelease(cachedObjects.Dequeue());
            }
        }

        protected abstract T Create();

        protected virtual void OnSpawn(T obj) { }

        protected virtual void OnRecycle(T obj) { }

        protected virtual void OnRelease(T obj) { }
    }
}
