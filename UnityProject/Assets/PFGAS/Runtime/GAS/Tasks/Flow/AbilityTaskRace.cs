using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace PFGAS.Runtime
{
    /// <summary>
    /// AbilityTaskRace 获胜任务的完成结果。
    /// </summary>
    public enum AbilityTaskRaceOutcome
    {
        Succeeded,
        Failed,
        Cancelled
    }

    /// <summary>
    /// 描述 AbilityTaskRace 解析出的获胜任务和相关事件数据。
    /// </summary>
    public sealed class AbilityTaskRaceResult
    {
        internal AbilityTaskRaceResult(
            string key,
            AbilityTask task,
            AbilityTaskRaceOutcome outcome,
            bool hasGameplayEvent,
            GameplayEvent gameplayEvent)
        {
            Key = key ?? string.Empty;
            Task = task;
            Outcome = outcome;
            HasGameplayEvent = hasGameplayEvent;
            GameplayEvent = gameplayEvent;
        }

        public string Key { get; }

        public AbilityTask Task { get; }

        public AbilityTaskRaceOutcome Outcome { get; }

        public bool Succeeded => Outcome == AbilityTaskRaceOutcome.Succeeded;

        public bool Failed => Outcome == AbilityTaskRaceOutcome.Failed;

        public bool WasCancelled => Outcome == AbilityTaskRaceOutcome.Cancelled;

        public bool HasGameplayEvent { get; }

        public GameplayEvent GameplayEvent { get; }

        public bool TryGetGameplayEvent<TPayload, TTargetData>(
            out GameplayEvent<TPayload, TTargetData> gameplayEvent)
            where TPayload : IGameplayEventPayload
            where TTargetData : IAbilityTargetData
        {
            if (!HasGameplayEvent)
            {
                gameplayEvent = default;
                return false;
            }

            gameplayEvent = GameplayEvent.As<TPayload, TTargetData>();
            return true;
        }

        public bool TryGetPayload<TPayload>(out TPayload payload)
            where TPayload : IGameplayEventPayload
        {
            if (HasGameplayEvent && GameplayEvent.TryGetPayload(out payload))
            {
                return true;
            }

            payload = default;
            return false;
        }

        public bool TryGetTargetData<TTargetData>(out TTargetData targetData)
            where TTargetData : IAbilityTargetData
        {
            if (HasGameplayEvent && GameplayEvent.TryGetTargetData(out targetData))
            {
                return true;
            }

            targetData = default;
            return false;
        }
    }

    /// <summary>
    /// 并行启动多个 AbilityTask，并以第一个完成的任务解析结果。
    /// </summary>
    public sealed class AbilityTaskRace : IDisposable
    {
        private readonly AbilityTaskGroup group;
        private readonly List<RaceEntry> entries = new List<RaceEntry>();
        private readonly Dictionary<AbilityTask, RaceEntry> entriesByTask = new Dictionary<AbilityTask, RaceEntry>();
        private readonly Dictionary<string, RaceEntry> entriesByKey = new Dictionary<string, RaceEntry>();

        private bool disposed;
        private bool resolved;
        private AbilityTaskRaceResult result;

        internal AbilityTaskRace(AbilityTaskGroup group)
        {
            this.group = group;
        }

        public AbilityExecution Execution => group.Execution;

        public AbilityTaskGroup Group => group;

        public bool IsDisposed => disposed;

        public bool IsResolved => resolved;

        public AbilityTaskRaceResult Result => result;

        public int EntryCount => entriesByTask.Count;

        public event Action<AbilityTaskRaceResult> Resolved;

        public AbilityTaskRace OnResolved(Action<AbilityTaskRaceResult> callback)
        {
            if (callback == null)
            {
                return this;
            }

            if (resolved)
            {
                callback(result);
                return this;
            }

            ThrowIfDisposed();
            Resolved += callback;
            return this;
        }

        public T StartTask<T>(T task, string key = null) where T : AbilityTask
        {
            return StartTask(key, task);
        }

        public T StartTask<T>(string key, T task) where T : AbilityTask
        {
            ThrowIfDisposedOrResolved();
            key = key ?? string.Empty;
            if (entriesByTask.ContainsKey(task))
            {
                throw new InvalidOperationException("AbilityTask is already tracked by this AbilityTaskRace.");
            }

            if (!string.IsNullOrEmpty(key) && entriesByKey.ContainsKey(key))
            {
                throw new InvalidOperationException($"AbilityTaskRace already contains an entry with key '{key}'.");
            }

            var entry = new RaceEntry(key, task);
            entry.FinishedHandler = (finishedTask, succeeded) => OnTaskFinished(entry, finishedTask, succeeded);
            entries.Add(entry);
            entriesByTask.Add(task, entry);
            if (!string.IsNullOrEmpty(key))
            {
                entriesByKey.Add(key, entry);
            }

            task.Finished += entry.FinishedHandler;

            try
            {
                group.StartTask(task);
            }
            catch
            {
                Untrack(entry);
                throw;
            }

            return task;
        }

        public bool Cancel()
        {
            if (disposed)
            {
                return false;
            }

            DetachAll();
            Exception cleanupException = null;
            try
            {
                group.Cancel();
            }
            catch (Exception ex)
            {
                cleanupException = ex;
            }
            finally
            {
                disposed = true;
                Resolved = null;
            }

            if (cleanupException != null)
            {
                ExceptionDispatchInfo.Capture(cleanupException).Throw();
            }

            return true;
        }

        public void Dispose()
        {
            Cancel();
        }

        private void OnTaskFinished(RaceEntry entry, AbilityTask task, bool succeeded)
        {
            if (disposed || resolved || entry == null || task == null || !ReferenceEquals(entry.Task, task))
            {
                return;
            }

            if (group.IsCancelling)
            {
                AbandonWithoutResolving();
                return;
            }

            Resolve(entry, task);
        }

        private void Resolve(RaceEntry entry, AbilityTask task)
        {
            resolved = true;
            result = CreateResult(entry, task);
            DetachAll();

            Exception cleanupException = null;
            try
            {
                group.Cancel();
            }
            catch (Exception ex)
            {
                cleanupException = ex;
            }
            finally
            {
                disposed = true;
            }

            try
            {
                Resolved?.Invoke(result);
            }
            finally
            {
                Resolved = null;
            }

            if (cleanupException != null)
            {
                ExceptionDispatchInfo.Capture(cleanupException).Throw();
            }
        }

        private void AbandonWithoutResolving()
        {
            DetachAll();
            disposed = true;
            Resolved = null;
        }

        private AbilityTaskRaceResult CreateResult(RaceEntry entry, AbilityTask task)
        {
            var outcome = task.WasCancelled
                ? AbilityTaskRaceOutcome.Cancelled
                : task.Succeeded
                    ? AbilityTaskRaceOutcome.Succeeded
                    : AbilityTaskRaceOutcome.Failed;

            if (task is WaitGameplayEventTask waitTask && waitTask.HasReceivedEvent)
            {
                return new AbilityTaskRaceResult(entry.Key, task, outcome, true, waitTask.ReceivedEvent);
            }

            return new AbilityTaskRaceResult(entry.Key, task, outcome, false, default);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AbilityTaskRace));
            }
        }

        private void ThrowIfDisposedOrResolved()
        {
            if (resolved)
            {
                throw new InvalidOperationException("AbilityTaskRace is already resolved.");
            }

            ThrowIfDisposed();
        }

        private void DetachAll()
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                Untrack(entries[i]);
            }
        }

        private void Untrack(RaceEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            entry.Task.Finished -= entry.FinishedHandler;
            entries.Remove(entry);
            entriesByTask.Remove(entry.Task);
            if (!string.IsNullOrEmpty(entry.Key))
            {
                entriesByKey.Remove(entry.Key);
            }
        }

        /// <summary>
        /// 保存 Race 中单个任务的键和完成回调。
        /// </summary>
        private sealed class RaceEntry
        {
            public RaceEntry(string key, AbilityTask task)
            {
                Key = key ?? string.Empty;
                Task = task;
            }

            public string Key { get; }

            public AbilityTask Task { get; }

            public Action<AbilityTask, bool> FinishedHandler { get; set; }
        }
    }
}
