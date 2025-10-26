/* 
****************************************************
* 文件：DebuggerManager.cs
* 作者：PeiFeng
* 创建时间：2025/10/25 18:54:49 星期六
* 功能：日志系统管理器
****************************************************
*/

using UnityEngine;

namespace PFDebugger
{
    public class DebuggerManager : MonoBehaviour
    {
        private static DebuggerManager instance;

        public static DebuggerManager I
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DebuggerManager>();

                    if (instance == null)
                        return null;
                }
                return instance;
            }
        }


        [SerializeField]
        private DebuggerActiveWindowType activeWindowType = DebuggerActiveWindowType.AlwaysOpen;
        public DebuggerActiveWindowType ActiveWindowType => activeWindowType;

        private Canvas canvas;
        public Canvas Canvas
        {
            get
            {
                if (canvas == null)
                    canvas = GetComponent<Canvas>();
                return canvas;
            }
        }

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
        
        private bool isActive;
        private DebuggerMiniBtn debuggerMiniBtn;
        private DebuggerMainWindow debuggerMainWindow;
        

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            debuggerMiniBtn = GetComponentInChildren<DebuggerMiniBtn>(true);
            debuggerMainWindow = GetComponentInChildren<DebuggerMainWindow>(true);

            if (debuggerMiniBtn == null || debuggerMainWindow == null)
                activeWindowType = DebuggerActiveWindowType.AlwaysClose;

            CheckActive();

            if (!isActive) return;

            debuggerMiniBtn.OnAwake();
            debuggerMainWindow.OnAwake();
        }

        private void Start()
        {
            ShowMainWindow(false);
        }

        private void Update()
        {
            if (!isActive) return;
            if (debuggerMiniBtn != null && debuggerMiniBtn.IsActive)
                debuggerMiniBtn.OnUpdate();
            if (debuggerMainWindow != null && debuggerMainWindow.IsActive)
                debuggerMainWindow.OnUpdate();
        }

        private void OnDestroy()
        {
            if (!isActive) return;
            OnRelease();
        }

        private void OnRelease()
        {   
            debuggerMiniBtn?.OnRelease();
            debuggerMainWindow?.OnRelease();
        }


        private void CheckActive()
        {
            isActive = activeWindowType switch
            {
                DebuggerActiveWindowType.AlwaysOpen => true,
                DebuggerActiveWindowType.OnlyOpenWhenDevelopment => Debug.isDebugBuild,
                DebuggerActiveWindowType.OnlyOpenInEditor => Application.isEditor,
                DebuggerActiveWindowType.AlwaysClose => false,
                _ => false,
            };
        }

        public void ShowMainWindow(bool isShow)
        {
            if (debuggerMiniBtn == null || debuggerMainWindow == null) return;
            debuggerMainWindow.IsActive = isShow;
            debuggerMiniBtn.IsActive = !isShow;
        }
    }
}