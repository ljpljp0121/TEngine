using UnityEngine;

namespace PFDebugger
{
    public static class Debugger
    {
        private static FPSCounterManager fpsCounterManager;
        private static LogManager logManager;
        private static GmManager gmManager;

        public static FPSCounterManager FPSCounterManager
        {
            get
            {
                fpsCounterManager ??= DebuggerManager.I?.GetManager<FPSCounterManager>();
                return fpsCounterManager;
            }
        }

        public static LogManager LogManager
        {
            get
            {
                logManager ??= DebuggerManager.I?.GetManager<LogManager>();
                return logManager;
            }
        }

        public static GmManager GmManager
        {
            get
            {
                gmManager ??= DebuggerManager.I?.GetManager<GmManager>();
                return gmManager;
            }
        }

        public static void VisibleMiniWindow()
        {
            DebuggerManager.I?.VisibleMainWindow(false);
            DebuggerManager.I?.VisibleMiniWindow(true);
        }

        public static void VisibleMainWindow()
        {
            DebuggerManager.I?.VisibleMainWindow(true);
            DebuggerManager.I?.VisibleMiniWindow(false);
        }

        public static DebuggerMiniBtn DebuggerMiniBtn => DebuggerManager.I?.DebuggerMiniBtn;
        public static DebuggerMainWindow DebuggerMainWindow => DebuggerManager.I?.DebuggerMainWindow;
    }
}