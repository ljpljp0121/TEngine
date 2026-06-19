using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 在同一 AbilityExecution 内局部分组并统一清理多个 AbilityTask。
    /// </summary>
    public sealed class AbilityTaskGroup : IDisposable
    {
        private readonly AbilityExecution execution;
        private readonly List<AbilityTask> tasks = new List<AbilityTask>();
        private readonly List<AbilityTask> cleanupTasks = new List<AbilityTask>();
        private readonly Dictionary<AbilityTask, Action<AbilityTask, bool>> finishedHandlers =
            new Dictionary<AbilityTask, Action<AbilityTask, bool>>();

        private bool disposed;
        private bool cancelling;

        internal AbilityTaskGroup(AbilityExecution execution)
        {
            this.execution = execution;
        }

        public AbilityExecution Execution => execution;

        public IReadOnlyList<AbilityTask> Tasks => tasks.ToArray();

        public int TaskCount => tasks.Count;

        public bool IsDisposed => disposed;

        public bool IsCancelling => cancelling;

        public T StartTask<T>(T task) where T : AbilityTask
        {
            ThrowIfDisposed();
            if (finishedHandlers.ContainsKey(task))
            {
                throw new InvalidOperationException("AbilityTask is already tracked by this AbilityTaskGroup.");
            }

            Action<AbilityTask, bool> finishedHandler = (finishedTask, _) => Untrack(finishedTask);
            tasks.Add(task);
            finishedHandlers.Add(task, finishedHandler);
            task.Finished += finishedHandler;

            try
            {
                execution.StartTask(task);
            }
            catch
            {
                Untrack(task);
                throw;
            }

            if (task.IsCompleted)
            {
                Untrack(task);
            }

            return task;
        }

        public bool Contains(AbilityTask task)
        {
            return task != null && finishedHandlers.ContainsKey(task);
        }

        public void Dispose()
        {
            Cancel();
        }

        public bool Cancel()
        {
            if (disposed)
            {
                return false;
            }

            Exception firstException = null;
            cancelling = true;
            try
            {
                cleanupTasks.AddRange(tasks);
                foreach (var task in cleanupTasks)
                {
                    if (task == null || task.IsCompleted)
                    {
                        continue;
                    }

                    try
                    {
                        task.Cancel();
                    }
                    catch (Exception ex)
                    {
                        firstException = firstException ?? ex;
                    }
                }
            }
            finally
            {
                cleanupTasks.Clear();
                cancelling = false;
                Release();
            }

            if (firstException != null)
            {
                ExceptionDispatchInfo.Capture(firstException).Throw();
            }

            return true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AbilityTaskGroup));
            }
        }

        private void Untrack(AbilityTask task)
        {
            if (task == null)
            {
                return;
            }

            if (finishedHandlers.TryGetValue(task, out var finishedHandler))
            {
                task.Finished -= finishedHandler;
                finishedHandlers.Remove(task);
            }

            tasks.Remove(task);
        }

        private void Release()
        {
            if (disposed)
            {
                return;
            }

            foreach (var pair in finishedHandlers)
            {
                pair.Key.Finished -= pair.Value;
            }

            finishedHandlers.Clear();
            tasks.Clear();
            disposed = true;
            execution.NotifyTaskGroupDisposed(this);
        }
    }
}
