using System;
using System.Collections.Generic;
using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    [DefaultExecutionOrder(-1000)]
    public class DebuggerManager : MonoSingleton<DebuggerManager>
    {
        public Font Font;
        
        private readonly List<SubManagerBase> managers = new();

        private readonly Dictionary<Type, SubManagerBase> managerMap = new();

        private RectTransform rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = GetComponent<RectTransform>();
                return rectTransform;
            }
        }

        private CanvasScaler canvasScaler;
        public CanvasScaler CanvasScaler
        {
            get
            {
                if (canvasScaler == null)
                    canvasScaler = GetComponent<CanvasScaler>();
                return canvasScaler;
            }
        }

        protected override void OnLoad()
        {
            var registrations = ManagerDiscovery.DiscoverManagers();

            foreach (var reg in registrations)
            {
                var instance = (SubManagerBase)Activator.CreateInstance(reg.Type);
                managers.Add(instance);
                managerMap[reg.Type] = instance;
            }

            foreach (var mgr in managers)
            {
                mgr.Init();
            }

            foreach (var mgr in managers)
            {
                mgr.PostInit();
            }

            debuggerMiniBtn = GetComponentInChildren<DebuggerMiniBtn>(true);
            debuggerMainWindow = GetComponentInChildren<DebuggerMainWindow>(true);
            debuggerMiniBtn.OnInit();
            debuggerMainWindow.OnInit();
        }

        private void Start()
        {
            VisibleMiniWindow(true);
            VisibleMainWindow(false);
        }

        private void Update()
        {
            if (managers.Count == 0) return;

            for (int i = 0; i < managers.Count; i++)
            {
                managers[i].Tick(Time.deltaTime, Time.unscaledDeltaTime);
            }
            if (debuggerMiniBtn.IsActive)
                debuggerMiniBtn.Tick();
            if (debuggerMainWindow.IsActive)
                debuggerMainWindow.Tick();
        }

        protected override void OnDestroy()
        {
            for (int i = managers.Count - 1; i >= 0; i--)
            {
                managers[i].DeInit();
            }
            managers.Clear();
            managerMap.Clear();
            debuggerMiniBtn.OnDeInit();
            debuggerMainWindow.OnDeInit();
            base.OnDestroy();
        }

        public T GetManager<T>() where T : SubManagerBase
        {
            if (managerMap.TryGetValue(typeof(T), out var mgr))
                return (T)mgr;
            throw new InvalidOperationException($"{typeof(T).Name} 没有查找到" +
                                                $"确保添加了 [SubManager] 特性");
        }

        public bool TryGetManager<T>(out T manager) where T : SubManagerBase
        {
            if (managerMap.TryGetValue(typeof(T), out var mgr))
            {
                manager = (T)mgr;
                return true;
            }
            manager = null;
            return false;
        }


        /// <summary> 迷你按钮 </summary>
        private DebuggerMiniBtn debuggerMiniBtn;
        /// <summary> 主界面 </summary>
        private DebuggerMainWindow debuggerMainWindow;

        public DebuggerMiniBtn DebuggerMiniBtn => debuggerMiniBtn;
        public DebuggerMainWindow DebuggerMainWindow => debuggerMainWindow;

        public void VisibleMiniWindow(bool visible)
        {
            if (debuggerMiniBtn == null) return;
            debuggerMiniBtn.IsActive = visible;
        }

        public void VisibleMainWindow(bool visible)
        {
            if (debuggerMainWindow == null) return;
            debuggerMainWindow.IsActive = visible;
        }
    }
}