using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PFDebugger
{
    /// <summary>LogManager 全面测试脚本，挂到场景任意 GameObject 上即可运行</summary>
    public class LogManagerTest : MonoBehaviour
    {
        private LogManager logManager;
        private int passCount;
        private int failCount;
        private bool logChanged;

        private void Start()
        {
            var debugger = DebuggerManager.I;
            logManager = debugger.GetManager<LogManager>();
            logManager.OnLogChanged += () => logChanged = true;
            StartCoroutine(RunAllTests());
        }

        private IEnumerator RunAllTests()
        {
            yield return null;

            yield return TestBasicCapture();
            yield return TestGetVisibleLogBoundary();
            yield return TestSetFilter();
            yield return TestToggleFilter();
            yield return TestIdempotentOperations();
            yield return TestSearch();
            yield return TestCollapse();
            yield return TestLogChangedEvent();
            yield return TestOverflowEviction();
            yield return TestClearAndResume();
            yield return TestLogEntryData();
            yield return TestPerformance();
            yield return TestMultiThread();

            PrintSummary();
        }

        // ================================================================
        // A. 基本捕获与计数
        // ================================================================

        private IEnumerator TestBasicCapture()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            // 1. Info/Warning/Error 分别捕获，计数准确
            Debug.Log("[TestA] info");
            Debug.LogWarning("[TestA] warn");
            Debug.LogError("[TestA] error");
            yield return null;

            Assert(logManager.InfoCount >= 1, "A1: InfoCount >= 1");
            Assert(logManager.WarningCount >= 1, "A1: WarningCount >= 1");
            Assert(logManager.ErrorCount >= 1, "A1: ErrorCount >= 1");

            // 2. LogType.Assert 归入 ErrorCount
            logManager.Clear();
            yield return null;
            Debug.LogAssertion("[TestA] assert");
            yield return null;
            Assert(logManager.ErrorCount >= 1, "A2: Assert counted as Error");

            // 3. TotalCount == Info + Warning + Error
            logManager.Clear();
            yield return null;
            Debug.Log("[TestA] i1");
            Debug.Log("[TestA] i2");
            Debug.LogWarning("[TestA] w1");
            Debug.LogError("[TestA] e1");
            yield return null;
            Assert(logManager.TotalCount == logManager.InfoCount + logManager.WarningCount + logManager.ErrorCount,
                "A3: TotalCount == Info + Warning + Error");

            // 4. 空字符串日志
            logManager.Clear();
            yield return null;
            Debug.Log("");
            yield return null;
            Assert(logManager.TotalCount >= 1, "A4: Empty string log captured");

            // 5. 特殊字符日志
            logManager.Clear();
            yield return null;
            Debug.Log("[TestA] \u4e2d\u6587\u65e5\u5fd7\n\ttab");
            yield return null;
            Assert(logManager.TotalCount >= 1, "A5: Special chars log captured");
            var entry5 = logManager.GetVisibleLog(logManager.VisibleCount - 1);
            Assert(entry5 != null && entry5.LogString.Contains("\u4e2d\u6587"), "A5: Chinese chars preserved");
        }

        // ================================================================
        // B. GetVisibleLog 边界
        // ================================================================

        private IEnumerator TestGetVisibleLogBoundary()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            Debug.Log("[TestB] visible");
            yield return null;

            // 6. 索引 0 有效
            Assert(logManager.GetVisibleLog(0) != null, "B6: GetVisibleLog(0) != null");

            // 7. 负索引返回 null
            Assert(logManager.GetVisibleLog(-1) == null, "B7: Negative index returns null");

            // 8. 超出范围返回 null
            Assert(logManager.GetVisibleLog(logManager.VisibleCount) == null, "B8: Out of range returns null");
            Assert(logManager.GetVisibleLog(99999) == null, "B8: Large index returns null");
        }

        // ================================================================
        // C. 过滤 - SetFilter
        // ================================================================

        private IEnumerator TestSetFilter()
        {
            logManager.Clear();
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            Debug.Log("[TestC] info");
            Debug.LogWarning("[TestC] warn");
            Debug.LogError("[TestC] error");
            yield return null;

            int total = logManager.VisibleCount;

            // 9. 仅 Info
            logManager.SetFilter(LogLevel.Info);
            yield return null;
            bool allInfo = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogType != LogType.Log) allInfo = false;
            }
            Assert(allInfo && logManager.VisibleCount > 0, "C9: SetFilter(Info) shows only Info");

            // 10. 仅 Error
            logManager.SetFilter(LogLevel.Error);
            yield return null;
            bool allError = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogType == LogType.Log) allError = false;
            }
            Assert(allError && logManager.VisibleCount > 0, "C10: SetFilter(Error) shows only Error");

            // 11. 组合过滤
            logManager.SetFilter(LogLevel.Warning | LogLevel.Error);
            yield return null;
            bool noInfo = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogType == LogType.Log) noInfo = false;
            }
            Assert(noInfo && logManager.VisibleCount > 0, "C11: Combined filter excludes Info");

            // 12. None 过滤
            logManager.SetFilter(LogLevel.None);
            yield return null;
            Assert(logManager.VisibleCount == 0, "C12: SetFilter(None) shows nothing");

            logManager.SetFilter(LogLevel.All);
            yield return null;
        }

        // ================================================================
        // D. 过滤 - ToggleFilter
        // ================================================================

        private IEnumerator TestToggleFilter()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            Debug.Log("[TestD] info");
            Debug.LogWarning("[TestD] warn");
            Debug.LogError("[TestD] error");
            yield return null;

            int allVisible = logManager.VisibleCount;

            // 13. 关闭再打开 Info
            logManager.ToggleFilter(LogLevel.Info);
            yield return null;
            int withoutInfo = logManager.VisibleCount;
            Assert(withoutInfo < allVisible, "D13: ToggleFilter removes Info");

            logManager.ToggleFilter(LogLevel.Info);
            yield return null;
            // Assert 自身的 Debug.LogError 会额外产生日志，不能用精确计数
            bool infoRestored = false;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogString.Contains("[TestD] info")) { infoRestored = true; break; }
            }
            Assert(infoRestored, "D13: ToggleFilter restores Info");

            // 14. 位运算组合
            logManager.SetFilter(LogLevel.All);
            yield return null;
            logManager.ToggleFilter(LogLevel.Info | LogLevel.Warning);
            yield return null;
            bool onlyErrors = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogType == LogType.Log) onlyErrors = false;
                if (e != null && e.LogType == LogType.Warning) onlyErrors = false;
            }
            Assert(onlyErrors, "D14: ToggleFilter(Info|Warning) removes both");

            // 15. 从 None 打开
            logManager.SetFilter(LogLevel.None);
            yield return null;
            Assert(logManager.VisibleCount == 0, "D15: None => 0 visible");
            logManager.ToggleFilter(LogLevel.Info);
            yield return null;
            bool hasInfo = false;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogType == LogType.Log) hasInfo = true;
            }
            Assert(hasInfo, "D15: ToggleFilter opens Info from None");

            logManager.SetFilter(LogLevel.All);
            yield return null;
        }

        // ================================================================
        // E. 幂等/空操作
        // ================================================================

        private IEnumerator TestIdempotentOperations()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            Debug.Log("[TestE] data");
            yield return null;

            logChanged = false;

            // 16. SetFilter 相同值不触发事件
            logManager.SetFilter(LogLevel.All);
            Assert(!logChanged, "E16: SetFilter same value doesn't fire event");

            // 17. SetCollapse 相同值
            logManager.SetCollapse(false);
            Assert(!logChanged, "E17: SetCollapse same value doesn't fire event");

            // 18. SetSearchTerm 相同值
            logManager.SetSearchTerm("");
            Assert(!logChanged, "E18: SetSearchTerm same value doesn't fire event");

            // 19. 连续多次 Clear
            logManager.Clear();
            logManager.Clear();
            logManager.Clear();
            yield return null;
            Assert(logManager.TotalCount == 0, "E19: Multiple Clear safe");
        }

        // ================================================================
        // F. 搜索
        // ================================================================

        private IEnumerator TestSearch()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            yield return null;

            Debug.Log("[TestF] apple_banana_cherry");
            Debug.LogWarning("[TestF] DOG_CAT_FISH");
            yield return null;

            // 20. 搜索匹配 logString
            logManager.SetSearchTerm("apple");
            yield return null;
            Assert(logManager.VisibleCount >= 1, "F20: Search 'apple' finds match");

            // 21. 搜索大小写不敏感
            logManager.SetSearchTerm("dog");
            yield return null;
            Assert(logManager.VisibleCount >= 1, "F21: Search 'dog' finds DOG (case insensitive)");

            // 22. 搜索不匹配
            logManager.SetSearchTerm("NONEXISTENT_KEYWORD_XYZ_999");
            yield return null;
            Assert(logManager.VisibleCount == 0, "F22: No match returns 0");

            // 23. null / 空字符串退出搜索
            logManager.SetSearchTerm(null);
            yield return null;
            Assert(!logManager.IsInSearchMode, "F23: SetSearchTerm(null) exits search");

            logManager.SetSearchTerm("test");
            yield return null;
            Assert(logManager.IsInSearchMode, "F23: SetSearchTerm enters search");

            logManager.SetSearchTerm("   ");
            yield return null;
            Assert(!logManager.IsInSearchMode, "F23: Whitespace-only treated as empty");
        }

        // ================================================================
        // G. 折叠
        // ================================================================

        private IEnumerator TestCollapse()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetSearchTerm("");
            yield return null;

            // 24. 折叠合并相同日志 Count 累加
            for (int i = 0; i < 3; i++)
                LogDuplicate("[TestG] same log");
            Debug.Log("[TestG] unique log");
            yield return null;

            logManager.SetCollapse(true);
            yield return null;

            bool foundCollapsed = false;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogString.Contains("same log") && e.Count == 3)
                {
                    foundCollapsed = true;
                    break;
                }
            }
            Assert(foundCollapsed, "G24: Collapse merges duplicates (Count==3)");

            // 25. 折叠模式下新增不重复日志正常显示
            int before = logManager.VisibleCount;
            Debug.Log("[TestG] another unique");
            yield return null;
            Assert(logManager.VisibleCount > before, "G25: New unique log visible in collapse mode");

            // 26. 取消折叠恢复完整列表
            logManager.SetCollapse(false);
            yield return null;
            bool hasAll = true;
            int uniqueCount = 0;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogString.Contains("same log")) uniqueCount++;
            }
            Assert(uniqueCount == 3, "G26: Uncollapse restores 3 duplicates");

            // 27. 折叠 + 过滤
            logManager.Clear();
            yield return null;
            Debug.LogWarning("[TestG] warn dup");
            Debug.LogWarning("[TestG] warn dup");
            Debug.LogError("[TestG] error dup");
            Debug.LogError("[TestG] error dup");
            yield return null;

            logManager.SetCollapse(true);
            logManager.SetFilter(LogLevel.Error);
            yield return null;
            bool onlyErr = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogType != LogType.Error && e.LogType != LogType.Assert && e.LogType != LogType.Exception)
                    onlyErr = false;
            }
            Assert(onlyErr && logManager.VisibleCount > 0, "G27: Collapse + filter works");

            // 28. 折叠 + 搜索
            logManager.SetFilter(LogLevel.All);
            logManager.SetSearchTerm("warn");
            yield return null;
            bool allContainWarn = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && !e.LogString.Contains("warn")) allContainWarn = false;
            }
            Assert(allContainWarn, "G28: Collapse + search works");

            logManager.SetSearchTerm("");
            logManager.SetCollapse(false);
            yield return null;
        }

        // ================================================================
        // H. OnLogChanged 事件
        // ================================================================

        private IEnumerator TestLogChangedEvent()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            // 29. 新增日志触发事件
            logChanged = false;
            Debug.Log("[TestH] trigger event");
            yield return null;
            Assert(logChanged, "H29: New log fires OnLogChanged");

            // 30. Clear 触发事件
            logChanged = false;
            logManager.Clear();
            yield return null;
            Assert(logChanged, "H30: Clear fires OnLogChanged");
        }

        // ================================================================
        // I. 溢出淘汰
        // ================================================================

        private IEnumerator TestOverflowEviction()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            int oldMax = logManager.MaxLogCount;
            int oldRemove = logManager.LogsToRemoveOnOverflow;
            logManager.MaxLogCount = 50;
            logManager.LogsToRemoveOnOverflow = 5;

            // 发送超过 MaxLogCount 的日志
            for (int i = 0; i < 70; i++)
            {
                Debug.Log($"[TestI] overflow_{i}");
            }
            yield return null;

            // 32. 自动淘汰
            Assert(logManager.TotalCount <= 50, "I32: TotalCount <= MaxLogCount after overflow");
            Assert(logManager.VisibleCount <= 50, "I32: VisibleCount <= MaxLogCount after overflow");

            // 33. 淘汰后计数正确
            int sum = logManager.InfoCount + logManager.WarningCount + logManager.ErrorCount;
            Assert(sum == logManager.TotalCount, "I33: Counts consistent after eviction");

            // 34. 淘汰后 VisibleCount 一致
            Assert(logManager.VisibleCount == logManager.TotalCount, "I34: VisibleCount == TotalCount (no filter)");

            // 35. 淘汰时过滤模式下 visibleEntries 正确裁剪
            logManager.Clear();
            logManager.SetFilter(LogLevel.Warning | LogLevel.Error);
            yield return null;

            // 交替发送 Info / Warning / Error，只有 Warning 和 Error 可见
            for (int i = 0; i < 70; i++)
            {
                if (i % 3 == 0)
                    Debug.Log($"[TestI] info_{i}");
                else if (i % 3 == 1)
                    Debug.LogWarning($"[TestI] warn_{i}");
                else
                    Debug.LogError($"[TestI] error_{i}");
            }
            yield return null;

            // visible 里不应包含任何 Info
            bool noInfoInVisible = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogType == LogType.Log)
                    noInfoInVisible = false;
            }
            Assert(noInfoInVisible, "I35: No Info in visible after eviction with filter");

            // visible 数量应等于 Warning + Error 计数
            int expectedVisible = logManager.WarningCount + logManager.ErrorCount;
            Assert(logManager.VisibleCount == expectedVisible, "I35: VisibleCount == Warning + Error after eviction");

            // visible 里每条日志内容应有效（LogString 不为空）
            bool allValid = true;
            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e == null || string.IsNullOrEmpty(e.LogString))
                    allValid = false;
            }
            Assert(allValid, "I35: All visible entries valid after eviction with filter");

            logManager.SetFilter(LogLevel.All);
            logManager.MaxLogCount = oldMax;
            logManager.LogsToRemoveOnOverflow = oldRemove;
            yield return null;
        }

        // ================================================================
        // J. Clear 后恢复
        // ================================================================

        private IEnumerator TestClearAndResume()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            Debug.Log("[TestJ] before clear");
            yield return null;
            Assert(logManager.TotalCount > 0, "J: pre-check has data");

            logManager.Clear();
            yield return null;

            // 35. Clear 后继续写，计数器从 0 累加
            // 不使用 Assert 检查 TotalCount==0，避免其 Debug.LogError 干扰后续计数
            if (logManager.TotalCount == 0) passCount++;
            else { failCount++; Debug.LogError("<color=red>[FAIL] J35: Clear resets TotalCount</color>"); }
            Debug.Log("[TestJ] after clear 1");
            Debug.LogWarning("[TestJ] after clear 2");
            yield return null;
            Assert(logManager.TotalCount == 2, "J35: Count resumes from 0");
            Assert(logManager.InfoCount == 1, "J35: InfoCount resumes from 0");
            Assert(logManager.WarningCount == 1, "J35: WarningCount resumes from 0");

            // 36. Clear 后过滤/搜索/折叠状态不变
            logManager.SetFilter(LogLevel.Error);
            logManager.SetSearchTerm("test");
            logManager.SetCollapse(true);
            logManager.Clear();
            Assert(logManager.Filter == LogLevel.Error, "J36: Filter preserved after Clear");
            Assert(logManager.SearchTerm == "test", "J36: SearchTerm preserved after Clear");
            Assert(logManager.CollapseEnabled, "J36: CollapseEnabled preserved after Clear");

            logManager.SetFilter(LogLevel.All);
            logManager.SetSearchTerm("");
            logManager.SetCollapse(false);
            yield return null;
        }

        // ================================================================
        // K. 性能测试
        // ================================================================

        private IEnumerator TestPerformance()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            // --- K37: 5000 条日志吞吐 (分离 Unity 开销 vs LogManager 开销) ---
            logManager.MaxLogCount = 10000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
                Debug.Log($"[TestK] bulk_{i}");
            long logCallMs = sw.ElapsedMilliseconds;

            // 等一帧让 Tick 处理完 pendingLogs，再单独计 Tick 耗时
            sw.Restart();
            yield return null;
            long tickMs = sw.ElapsedMilliseconds;

            Debug.LogWarning($"<color=cyan>[PERF] K37: Debug.Log x5000 = {logCallMs}ms, Tick process = {tickMs}ms</color>");

            // --- K38: RebuildVisibleEntries 1000 条 x10 次 ---
            logManager.Clear();
            yield return null;
            logManager.MaxLogCount = 2000;
            for (int i = 0; i < 1000; i++)
                Debug.Log($"[TestK] rebuild_{i}");
            yield return null;

            sw.Restart();
            for (int i = 0; i < 10; i++)
            {
                logManager.SetFilter(LogLevel.Error);
                logManager.SetFilter(LogLevel.All);
            }
            sw.Stop();
            Debug.LogWarning($"<color=cyan>[PERF] K38: 10x Rebuild 1000 entries = {sw.ElapsedMilliseconds}ms</color>");

            // --- K39: 折叠 + 搜索 1000 条重复 ---
            logManager.Clear();
            yield return null;
            for (int i = 0; i < 1000; i++)
                LogDuplicate("[TestK] repeated log");
            yield return null;

            logManager.SetCollapse(true);
            yield return null;

            sw.Restart();
            logManager.SetSearchTerm("repeated");
            logManager.SetSearchTerm("");
            logManager.SetSearchTerm("nonexistent");
            logManager.SetSearchTerm("");
            sw.Stop();
            Debug.LogWarning($"<color=cyan>[PERF] K39: Collapse + 4x search = {sw.ElapsedMilliseconds}ms</color>");

            logManager.MaxLogCount = 1000;
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;
        }

        // ================================================================
        // L. 日志条目数据验证
        // ================================================================

        private IEnumerator TestLogEntryData()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            yield return null;

            Debug.Log("[TestL] data check");
            yield return null;

            var entry = logManager.GetVisibleLog(logManager.VisibleCount - 1);

            // 40. Timestamp 有值
            Assert(entry != null, "L40: Entry exists");
            Assert(entry != null && entry.Timestamp.frameCount > 0, "L40: Timestamp.frameCount > 0");
            Assert(entry != null && entry.Timestamp.elapsedSeconds > 0f, "L40: Timestamp.elapsedSeconds > 0");

            // 41. LogString / StackTrace 内容正确
            Assert(entry != null && entry.LogString.Contains("[TestL] data check"), "L41: LogString correct");
            Assert(entry != null && !string.IsNullOrEmpty(entry.StackTrace), "L41: StackTrace not empty");
        }

        // ================================================================
        // 辅助方法
        // ================================================================

        private static void LogDuplicate(string msg)
        {
            Debug.Log(msg);
        }

        private void Assert(bool condition, string message)
        {
            if (condition)
            {
                passCount++;
                Debug.LogError($"<color=yellow>[Suc] {message}</color>");
            }
            else
            {
                failCount++;
                Debug.LogError($"<color=red>[FAIL] {message}</color>");
            }
        }

        // ================================================================
        // M. 多线程测试
        // ================================================================

        private IEnumerator TestMultiThread()
        {
            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm("");
            logManager.MaxLogCount = 10000;
            yield return null;

            // 42. 多线程并发写入不崩溃
            int totalToWrite = 500;
            int written = 0;
            var doneEvent = new System.Threading.ManualResetEvent(false);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                for (int i = 0; i < totalToWrite; i++)
                    Debug.Log($"[TestM] thread_log_{i}");
                System.Threading.Interlocked.Exchange(ref written, totalToWrite);
                doneEvent.Set();
            });

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                for (int i = 0; i < totalToWrite; i++)
                    Debug.LogWarning($"[TestM] thread_warn_{i}");
                System.Threading.Interlocked.Add(ref written, totalToWrite);
                doneEvent.Set();
            });

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                for (int i = 0; i < totalToWrite; i++)
                    Debug.LogError($"[TestM] thread_error_{i}");
                System.Threading.Interlocked.Add(ref written, totalToWrite);
                doneEvent.Set();
            });

            // 等后台线程写完，再等一帧让 Tick 处理
            doneEvent.WaitOne(5000);
            yield return null;
            yield return null;

            Assert(logManager.TotalCount > 0, "M42: Multi-thread logs received");
            Assert(logManager.TotalCount <= 1500, "M42: TotalCount bounded by MaxLogCount");
            Assert(logManager.InfoCount > 0, "M42: InfoCount > 0 from thread");
            Assert(logManager.WarningCount > 0, "M42: WarningCount > 0 from thread");
            Assert(logManager.ErrorCount > 0, "M42: ErrorCount > 0 from thread");

            int sum = logManager.InfoCount + logManager.WarningCount + logManager.ErrorCount;
            Assert(sum == logManager.TotalCount, "M42: Counts consistent after multi-thread");

            // 43. 多线程写入的日志数据有效
            bool hasValidEntry = false;
            for (int i = 0; i < logManager.VisibleCount && !hasValidEntry; i++)
            {
                var e = logManager.GetVisibleLog(i);
                if (e != null && e.LogString.Contains("thread_"))
                    hasValidEntry = true;
            }
            Assert(hasValidEntry, "M43: Thread log data valid");

            logManager.MaxLogCount = 1000;
            yield return null;
        }

        private void PrintSummary()
        {
            string color = failCount == 0 ? "green" : "red";
            Debug.LogWarning($"<color={color}>======= LogManager Test: {passCount} passed, {failCount} failed =======</color>");
        }
    }
}
