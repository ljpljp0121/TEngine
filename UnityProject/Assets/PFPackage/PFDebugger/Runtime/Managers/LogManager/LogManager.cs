using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFDebugger
{
    /// <summary>日志管理器，负责日志捕获、去重折叠、过滤和搜索</summary>
    [SubManager(-50)]
    public class LogManager : SubManagerBase
    {
        /// <summary>最大日志条数（uncollapsed），超过时淘汰旧日志</summary>
        public int MaxLogCount { get; set; } = int.MaxValue;

        /// <summary>溢出时一次淘汰的条数</summary>
        public int LogsToRemoveOnOverflow { get; set; } = 16;

        /// <summary>当前日志级别过滤</summary>
        public LogLevel Filter { get; private set; } = LogLevel.All;

        /// <summary>当前搜索关键词</summary>
        public string SearchTerm { get; private set; }

        /// <summary>当前可见日志数量 (过滤 + 搜索 + 折叠) 之后</summary>
        public int VisibleCount => visibleEntries.Count;

        /// <summary>Info 级别日志计数</summary>
        public int InfoCount { get; private set; }

        /// <summary>Warning 级别日志计数</summary>
        public int WarningCount { get; private set; }

        /// <summary>Error 级别日志计数</summary>
        public int ErrorCount { get; private set; }

        /// <summary>总日志计数</summary>
        public int TotalCount { get; private set; }

        /// <summary>是否处于搜索模式</summary>
        public bool IsInSearchMode => !string.IsNullOrEmpty(SearchTerm);

        /// <summary>是否开启折叠模式</summary>
        public bool CollapseEnabled { get; private set; }

        /// <summary>日志数据变更事件，每帧最多触发一次</summary>
        public event Action OnLogChanged;
        
        public DynamicCircularBuffer<LogEntry> VisibleEntries => visibleEntries;

        private DynamicCircularBuffer<LogEntry> collapsedEntries;
        private DynamicCircularBuffer<LogEntry> uncollapsedEntries;
        private DynamicCircularBuffer<LogEntry> visibleEntries;
        private Dictionary<LogEntry, LogEntry> collapsedEntriesMap;

        private readonly Stack<LogEntry> entryPool = new Stack<LogEntry>(64);
        private const int MAX_POOL_SIZE = 4096;

        private readonly object logLock = new object();
        private readonly Queue<QueuedDebugLogEntry> pendingLogs = new Queue<QueuedDebugLogEntry>(32);

        private TimeSpan localTimeUtcOffset;
        private float lastElapsedSeconds;
        private int lastFrameCount;

        private Action<LogEntry> poolEntryAction;
        private Predicate<LogEntry> shouldRemoveCollapsedPredicate;
        private Action<LogEntry, int> updateCollapsedIndexAction;
        private readonly List<QueuedDebugLogEntry> batchBuffer = new List<QueuedDebugLogEntry>(64);
        private readonly List<LogEntry> deadEntries = new List<LogEntry>();

        public override void Init()
        {
            collapsedEntries = new DynamicCircularBuffer<LogEntry>(128);
            collapsedEntriesMap = new Dictionary<LogEntry, LogEntry>(128, new LogEntryContentEqualityComparer());
            uncollapsedEntries = new DynamicCircularBuffer<LogEntry>(256);
            visibleEntries = new DynamicCircularBuffer<LogEntry>(256);

            poolEntryAction = PoolEntry;
            shouldRemoveCollapsedPredicate = ShouldRemoveCollapsedEntry;
            updateCollapsedIndexAction = (entry, index) => entry.CollapsedIndex = index;

            localTimeUtcOffset = DateTime.Now - DateTime.UtcNow;

            Application.logMessageReceivedThreaded += ReceivedLog;
        }

        public override void DeInit()
        {
            Application.logMessageReceivedThreaded -= ReceivedLog;
            ClearInternal();
            entryPool.Clear();
        }

        private bool isDirty;

        public override void Tick(float elapseSeconds, float realElapseSeconds)
        {
            lastElapsedSeconds = Time.realtimeSinceStartup;
            lastFrameCount = Time.frameCount;
            ProcessPendingLogs();

            if (isDirty)
            {
                isDirty = false;
                OnLogChanged?.Invoke();
            }
        }

        private void ReceivedLog(string condition, string stackTrace, LogType logType)
        {
            lock (logLock)
            {
                if (pendingLogs.Count >= MaxLogCount)
                    pendingLogs.Dequeue();

                pendingLogs.Enqueue(new QueuedDebugLogEntry(condition, stackTrace, logType));
            }
        }

        private void ProcessPendingLogs()
        {
            if (pendingLogs.Count == 0) return;

            batchBuffer.Clear();
            lock (logLock)
            {
                while (pendingLogs.Count > 0)
                    batchBuffer.Add(pendingLogs.Dequeue());
            }

            foreach (var queued in batchBuffer)
            {
                IncrementLogTypeCount(queued.logType);
                ProcessLogEntry(queued);
            }

            if (uncollapsedEntries.Count > MaxLogCount)
            {
                int toRemove = Math.Min(
                    LogsToRemoveOnOverflow,
                    uncollapsedEntries.Count - MaxLogCount + LogsToRemoveOnOverflow);
                RemoveOldestLogs(toRemove);
            }
        }

        private void ProcessLogEntry(QueuedDebugLogEntry queued)
        {
            LogEntry entry = AllocateEntry();
            var timestamp = new DebugLogEntryTimestamp(
                DateTime.UtcNow + localTimeUtcOffset,
                lastElapsedSeconds,
                lastFrameCount);
            entry.Initialize(queued.logString, queued.stackTrace, queued.logType, timestamp);

            bool isDuplicate = collapsedEntriesMap.TryGetValue(entry, out var existingEntry);

            if (isDuplicate)
            {
                PoolEntry(entry);
                entry = existingEntry;
                entry.Count++;

                if (CollapseEnabled && MatchesFilter(entry))
                {
                    int visibleIndex = visibleEntries.IndexOf(entry);
                    if (visibleIndex >= 0)
                        isDirty = true;
                }
            }
            else
            {
                entry.CollapsedIndex = collapsedEntries.Count;
                collapsedEntries.Add(entry);
                collapsedEntriesMap[entry] = entry;
            }

            uncollapsedEntries.Add(entry);

            if (!isDuplicate || !CollapseEnabled)
            {
                if (MatchesFilter(entry))
                {
                    visibleEntries.Add(entry);
                    isDirty = true;
                }
            }
        }

        private void RemoveOldestLogs(int count)
        {
            if (count <= 0) return;

            bool anyCollapsedRemoved = false;
            int removedVisibleCount = 0;

            uncollapsedEntries.TrimStart(count, entry =>
            {
                if (--entry.Count <= 0)
                    anyCollapsedRemoved = true;
                DecrementLogTypeCount(entry.LogType);

                if (!CollapseEnabled && removedVisibleCount < visibleEntries.Count
                    && visibleEntries[removedVisibleCount] == entry)
                    removedVisibleCount++;
            });

            if (!CollapseEnabled && removedVisibleCount > 0)
                visibleEntries.TrimStart(removedVisibleCount);

            if (anyCollapsedRemoved)
            {
                collapsedEntries.RemoveAll(shouldRemoveCollapsedPredicate, updateCollapsedIndexAction);

                if (CollapseEnabled)
                {
                    visibleEntries.RemoveAll(entry => entry.Count <= 0);
                }

                for (int i = 0; i < deadEntries.Count; i++)
                    PoolEntry(deadEntries[i]);
                deadEntries.Clear();
            }

            isDirty = true;
        }

        /// <summary>按索引获取可见日志条目</summary>
        public LogEntry GetVisibleLog(int index)
        {
            if (index < 0 || index >= visibleEntries.Count)
                return null;
            return visibleEntries[index];
        }

        /// <summary>设置日志级别过滤</summary>
        public void SetFilter(LogLevel filter)
        {
            if (Filter == filter) return;
            Filter = filter;
            RebuildVisibleEntries();
            isDirty = true;
        }

        /// <summary>切换指定日志级别的显示状态</summary>
        public void ToggleFilter(LogLevel level)
        {
            Filter ^= level;
            RebuildVisibleEntries();
            isDirty = true;
        }

        /// <summary>设置搜索关键词，为空时退出搜索模式</summary>
        public void SetSearchTerm(string term)
        {
            term = term?.Trim() ?? string.Empty;
            if (SearchTerm == term) return;

            bool wasInSearchMode = IsInSearchMode;
            SearchTerm = term;

            if (IsInSearchMode || wasInSearchMode)
            {
                RebuildVisibleEntries();
                isDirty = true;
            }
        }

        /// <summary>开启或关闭折叠模式</summary>
        public void SetCollapse(bool enabled)
        {
            if (CollapseEnabled == enabled) return;
            CollapseEnabled = enabled;
            RebuildVisibleEntries();
            isDirty = true;
        }

        /// <summary>清空所有日志</summary>
        public void Clear()
        {
            ClearInternal();
            lock (logLock)
            {
                pendingLogs.Clear();
            }
            batchBuffer.Clear();
            InfoCount = 0;
            WarningCount = 0;
            ErrorCount = 0;
            TotalCount = 0;
            isDirty = true;
        }

        private void RebuildVisibleEntries()
        {
            visibleEntries.Clear();

            if (Filter == LogLevel.None) return;

            var source = CollapseEnabled ? collapsedEntries : uncollapsedEntries;

            if (Filter == LogLevel.All && !IsInSearchMode)
            {
                visibleEntries.AddRange(source);
                return;
            }

            bool isInfo = (Filter & LogLevel.Info) == LogLevel.Info;
            bool isWarning = (Filter & LogLevel.Warning) == LogLevel.Warning;
            bool isError = (Filter & LogLevel.Error) == LogLevel.Error;

            for (int i = 0; i < source.Count; i++)
            {
                var entry = source[i];

                if (IsInSearchMode && !entry.MatchesSearchTerm(SearchTerm))
                    continue;

                bool show = false;
                switch (entry.LogType)
                {
                    case LogType.Log:
                        show = isInfo;
                        break;
                    case LogType.Warning:
                        show = isWarning;
                        break;
                    default:
                        show = isError;
                        break;
                }

                if (show)
                    visibleEntries.Add(entry);
            }
        }

        private bool MatchesFilter(LogEntry entry)
        {
            if (Filter == LogLevel.None) return false;

            if (Filter != LogLevel.All)
            {
                bool match = false;
                switch (entry.LogType)
                {
                    case LogType.Log:
                        match = (Filter & LogLevel.Info) == LogLevel.Info;
                        break;
                    case LogType.Warning:
                        match = (Filter & LogLevel.Warning) == LogLevel.Warning;
                        break;
                    default:
                        match = (Filter & LogLevel.Error) == LogLevel.Error;
                        break;
                }
                if (!match) return false;
            }
            if (IsInSearchMode && !entry.MatchesSearchTerm(SearchTerm))
                return false;

            return true;
        }

        private void ClearInternal()
        {
            collapsedEntries.ForEach(poolEntryAction);
            collapsedEntries.Clear();
            collapsedEntriesMap.Clear();
            uncollapsedEntries.Clear();
            visibleEntries.Clear();
        }

        private LogEntry AllocateEntry()
        {
            return entryPool.Count > 0 ? entryPool.Pop() : new LogEntry();
        }

        private void PoolEntry(LogEntry entry)
        {
            if (entryPool.Count < MAX_POOL_SIZE)
            {
                entry.Clear();
                entryPool.Push(entry);
            }
        }

        private bool ShouldRemoveCollapsedEntry(LogEntry entry)
        {
            if (entry.Count <= 0)
            {
                collapsedEntriesMap.Remove(entry);
                deadEntries.Add(entry);
                return true;
            }
            return false;
        }

        private void IncrementLogTypeCount(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    InfoCount++;
                    break;
                case LogType.Warning:
                    WarningCount++;
                    break;
                default:
                    ErrorCount++;
                    break;
            }
            TotalCount++;
        }

        private void DecrementLogTypeCount(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    InfoCount = Math.Max(0, InfoCount - 1);
                    break;
                case LogType.Warning:
                    WarningCount = Math.Max(0, WarningCount - 1);
                    break;
                default:
                    ErrorCount = Math.Max(0, ErrorCount - 1);
                    break;
            }
            TotalCount = Math.Max(0, TotalCount - 1);
        }
    }
}
