/* 
****************************************************
* 文件：GameObjectPool.cs
* 作者：PeiFeng
* 创建时间：2025/10/26 00:25:04 星期日
* 功能：简易对象池
****************************************************
*/

using System.Collections.Generic;
using UnityEngine;

namespace PFDebugger
{
    public class GameObjectPool
    {
        private static GameObjectPool instance;
        public static GameObjectPool I => instance ??= new GameObjectPool();

        // 内部数据
        private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, Transform> poolRoots = new Dictionary<string, Transform>();
        private const int MAX_SIZE = 50;
        private Transform masterRoot; 

        public GameObjectPool()
        {
            // 创建统一的池根节点
            masterRoot = new GameObject("[GameObjectPool]").transform;
            masterRoot.localScale = Vector3.one;
            if (DebuggerManager.I != null)
                masterRoot.SetParent(DebuggerManager.I.transform);
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        public GameObject Get(GameObject prefab, Transform parent = null)
        {
            if (prefab == null) return null;

            string key = prefab.name;
            
            if (!pools.TryGetValue(key, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                pools[key] = pool;
            }

            // 从池取或新建
            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                obj.SetActive(true);
            }
            else
            {
                obj = Object.Instantiate(prefab);
                obj.name = key;
            }

            // 设置父级
            if (parent != null)
                obj.transform.SetParent(parent, false);

            return obj;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Release(GameObject obj)
        {
            if (obj == null) return;

            string key = obj.name;

            // 限制池大小
            if (!pools.TryGetValue(key, out Queue<GameObject> pool) || pool.Count >= MAX_SIZE)
            {
                Object.Destroy(obj);
                return;
            }

            // 重置并回收
            obj.SetActive(false);
            obj.transform.SetParent(GetPoolRoot(key));

            pool.Enqueue(obj);
        }

        /// <summary>
        /// 预热
        /// </summary>
        public void Warm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;

            string key = prefab.name;
            var root = GetPoolRoot(key);

            if (!pools.ContainsKey(key))
                pools[key] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                var obj = Object.Instantiate(prefab, root);
                obj.name = key;
                obj.SetActive(false);
                pools[key].Enqueue(obj);
            }
        }

        /// <summary>
        /// 释放整个对象池
        /// </summary>
        public void Clear()
        {
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    Object.Destroy(pool.Dequeue());
                }
            }
            
            foreach (var root in poolRoots.Values)
            {
                if (root != null)
                    Object.Destroy(root.gameObject);
            }

            if (masterRoot != null)
                Object.Destroy(masterRoot.gameObject);

            pools.Clear();
            poolRoots.Clear();
        }

        // 获取或创建池根节点
        private Transform GetPoolRoot(string key)
        {
            if (!poolRoots.TryGetValue(key, out Transform root))
            {
                var go = new GameObject($"Pool_{key}");
                go.transform.SetParent(masterRoot);
                go.transform.localScale = Vector3.one;
                go.SetActive(false);
                root = go.transform;
                poolRoots[key] = root;
            }
            return root;
        }
    }
}