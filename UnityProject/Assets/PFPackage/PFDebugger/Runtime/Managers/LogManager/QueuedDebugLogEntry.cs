using UnityEngine;

namespace PFDebugger
{
    /// <summary>后台线程接收的日志数据，用于线程间传递</summary>
    public struct QueuedDebugLogEntry
    {
        public readonly string logString;
        public readonly string stackTrace;
        public readonly LogType logType;

        public QueuedDebugLogEntry(string logString, string stackTrace, LogType logType)
        {
            this.logString = logString;
            this.stackTrace = stackTrace;
            this.logType = logType;
        }
    }
}
