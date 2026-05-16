using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PFDebugger
{
    /// <summary>日志级别位标记</summary>
    [Flags]
    public enum LogLevel
    {
        None    = 0,
        Info    = 1 << 0,
        Warning = 1 << 1,
        Error   = 1 << 2,
        All     = ~0
    }

    /// <summary>单条日志数据</summary>
    public class LogEntry
    {
        private const int HASH_NOT_CALCULATED = -623218;

        /// <summary>日志文本</summary>
        public string LogString { get; private set; }
        /// <summary>堆栈跟踪</summary>
        public string StackTrace { get; private set; }
        /// <summary>日志类型</summary>
        public LogType LogType { get; internal set; }
        /// <summary>折叠计数（相同日志出现的次数）</summary>
        public int Count { get; set; }
        /// <summary>在 collapsedEntries 中的索引</summary>
        public int CollapsedIndex { get; set; }
        /// <summary>记录时间</summary>
        public DebugLogEntryTimestamp Timestamp { get; set; }

        private int hashValue;
        private string completeLog;

        internal void Initialize(string logString, string stackTrace, LogType logType, DebugLogEntryTimestamp timestamp)
        {
            LogString = logString;
            StackTrace = stackTrace;
            LogType = logType;
            Count = 1;
            CollapsedIndex = -1;
            Timestamp = timestamp;
            hashValue = HASH_NOT_CALCULATED;
            completeLog = null;
        }

        internal void Clear()
        {
            LogString = null;
            StackTrace = null;
            completeLog = null;
            LogType = LogType.Log;
            Count = 0;
            CollapsedIndex = -1;
            Timestamp = default;
            hashValue = HASH_NOT_CALCULATED;
        }

        /// <summary>大小写不敏感搜索，同时匹配 logString 和 stackTrace</summary>
        public bool MatchesSearchTerm(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return true;

            var comparer = CultureInfo.InvariantCulture.CompareInfo;
            var options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

            if (LogString != null && comparer.IndexOf(LogString, searchTerm, options) >= 0)
                return true;

            if (StackTrace != null && comparer.IndexOf(StackTrace, searchTerm, options) >= 0)
                return true;

            return false;
        }
        
        public override string ToString()
        {
            completeLog ??= string.Concat(LogString, "\n", StackTrace);
            return completeLog;
        }

        internal int GetContentHashCode()
        {
            if (hashValue == HASH_NOT_CALCULATED)
            {
                unchecked
                {
                    hashValue = 17;
                    hashValue = hashValue * 23 + (LogString?.GetHashCode() ?? 0);
                    hashValue = hashValue * 23 + (StackTrace?.GetHashCode() ?? 0);
                }
            }

            return hashValue;
        }
    }

    /// <summary>LogEntry 内容相等比较器（按 logString + stackTrace 去重）</summary>
    internal class LogEntryContentEqualityComparer : EqualityComparer<LogEntry>
    {
        public override bool Equals(LogEntry x, LogEntry y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.LogString == y.LogString && x.StackTrace == y.StackTrace;
        }

        public override int GetHashCode(LogEntry obj)
        {
            return obj != null ? obj.GetContentHashCode() : 0;
        }
    }
}
