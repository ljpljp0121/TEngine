using UnityEngine;

namespace PFDebugger
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T instance;

        public static T I
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    Debug.LogError($"[PFDebugger] DebuggerManager应该挂载在 Debugger预制体根节点上，看看怎么回事");
                    return null;
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = (T)this;
            DontDestroyOnLoad(gameObject);
            OnLoad();
        }

        protected virtual void OnLoad() { }

        protected virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
