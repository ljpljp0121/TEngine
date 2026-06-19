using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 单次 Ability 激活产生的运行实例。
    /// </summary>
    public sealed class AbilityExecution
    {
        private readonly List<AbilityTask> activeTasks = new List<AbilityTask>();
        private readonly List<AbilityTask> cachedActiveTasks = new List<AbilityTask>();
        private readonly List<AbilityTask> cleanupActiveTasks = new List<AbilityTask>();
        private readonly List<AbilityTaskGroup> taskGroups = new List<AbilityTaskGroup>();
        private readonly List<AbilityTaskGroup> cleanupTaskGroups = new List<AbilityTaskGroup>();

        public AbilityExecution(
            AbilitySpec spec,
            IAbilityActivationRequest activationRequest = null)
        {
            Spec = spec;
            ActivationRequest = activationRequest ?? AbilityActivationRequest.Empty;
            Context = spec.Ability.CreateExecutionContext(this, ActivationRequest);
        }

        public AbilitySpec Spec { get; }

        public IAbilityActivationRequest ActivationRequest { get; }

        public AbilityExecutionContext Context { get; }

        public bool IsStarted { get; private set; }

        public bool IsCompleted { get; private set; }

        public bool Succeeded { get; private set; }

        public bool IsEnding { get; private set; }

        public bool WasCancelled { get; private set; }

        public bool IsCommitted { get; private set; }

        public IReadOnlyList<AbilityTask> ActiveTasks => activeTasks.ToArray();

        public int ActiveTaskCount => activeTasks.Count;

        public IReadOnlyList<AbilityTaskGroup> TaskGroups => taskGroups.ToArray();

        public int TaskGroupCount => taskGroups.Count;

        private bool ownsActiveState;

        public bool Start()
        {
            if (IsStarted)
            {
                return false;
            }

            IsStarted = true;
            if (!Spec.CanActivate)
            {
                Complete(false, false);
                return false;
            }

            Spec.MarkActive();
            ownsActiveState = true;
            try
            {
                Spec.Ability.Activate(Context);
            }
            catch
            {
                CancelActiveTasks();
                Complete(false, false);
                throw;
            }

            return true;
        }

        public bool Commit()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("Ability execution can only commit after it has started.");
            }

            if (IsCompleted || IsEnding)
            {
                throw new InvalidOperationException("Ability execution cannot commit after it has started ending or completed.");
            }

            if (IsCommitted)
            {
                return false;
            }

            IsCommitted = true;
            return true;
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (!IsStarted || IsCompleted)
            {
                return;
            }

            try
            {
                cachedActiveTasks.AddRange(activeTasks);
                foreach (var task in cachedActiveTasks)
                {
                    if (task == null || task.IsCompleted || !task.RequiresTick || !activeTasks.Contains(task))
                    {
                        continue;
                    }

                    task.Tick(deltaTime, unscaledDeltaTime);
                }
            }
            finally
            {
                cachedActiveTasks.Clear();
                RemoveCompletedTasks();
            }
        }

        public T StartTask<T>(T task) where T : AbilityTask
        {
            if (!IsStarted || IsCompleted || IsEnding)
            {
                throw new InvalidOperationException("Ability tasks can only start while their ability execution is active.");
            }

            task.AttachToExecution(this, Context);
            if (!activeTasks.Contains(task))
            {
                activeTasks.Add(task);
            }

            try
            {
                task.Start();
            }
            catch
            {
                activeTasks.Remove(task);
                throw;
            }

            if (task.IsCompleted)
            {
                activeTasks.Remove(task);
            }

            return task;
        }

        public AbilityTaskGroup CreateTaskGroup()
        {
            if (!IsStarted || IsCompleted || IsEnding)
            {
                throw new InvalidOperationException("Ability task groups can only be created while their ability execution is active.");
            }

            var group = new AbilityTaskGroup(this);
            taskGroups.Add(group);
            return group;
        }

        public AbilityTaskRace CreateTaskRace()
        {
            return new AbilityTaskRace(CreateTaskGroup());
        }

        internal void NotifyTaskCompleted(AbilityTask task)
        {
            if (task == null || !ReferenceEquals(task.OwningExecution, this))
            {
                return;
            }

            activeTasks.Remove(task);
        }

        internal void NotifyTaskCancelled(AbilityTask task)
        {
            NotifyTaskCompleted(task);
        }

        internal void NotifyTaskGroupDisposed(AbilityTaskGroup group)
        {
            if (group == null || !ReferenceEquals(group.Execution, this))
            {
                return;
            }

            taskGroups.Remove(group);
        }

        private void RemoveCompletedTasks()
        {
            for (var i = activeTasks.Count - 1; i >= 0; i--)
            {
                var task = activeTasks[i];
                if (task == null || task.IsCompleted)
                {
                    activeTasks.RemoveAt(i);
                }
            }
        }

        public void Cancel()
        {
            CancelAbility();
        }

        public bool EndAbility(bool succeeded = true)
        {
            if (IsCompleted)
            {
                return false;
            }

            Exception cleanupException;
            IsEnding = true;
            try
            {
                cleanupException = CancelActiveTasks();
                Complete(succeeded, false);
            }
            finally
            {
                IsEnding = false;
            }

            if (cleanupException != null)
            {
                ExceptionDispatchInfo.Capture(cleanupException).Throw();
            }

            return true;
        }

        public bool CancelAbility()
        {
            if (IsCompleted)
            {
                return false;
            }

            Exception cleanupException;
            IsEnding = true;
            try
            {
                cleanupException = CancelActiveTasks();
                Complete(false, true);
            }
            finally
            {
                IsEnding = false;
            }

            if (cleanupException != null)
            {
                ExceptionDispatchInfo.Capture(cleanupException).Throw();
            }

            return true;
        }

        private void Complete(bool succeeded, bool cancelled)
        {
            if (IsCompleted)
            {
                return;
            }

            var wasAlreadyEnding = IsEnding;
            IsEnding = true;
            try
            {
                WasCancelled = cancelled;
                IsCompleted = true;
                Succeeded = succeeded;
                if (ownsActiveState)
                {
                    ownsActiveState = false;
                    Spec.MarkInactive();
                }
            }
            finally
            {
                if (!wasAlreadyEnding)
                {
                    IsEnding = false;
                }
            }
        }

        private Exception CancelActiveTasks()
        {
            Exception firstException = null;

            try
            {
                cleanupTaskGroups.AddRange(taskGroups);
                foreach (var group in cleanupTaskGroups)
                {
                    if (group == null || group.IsDisposed || !ReferenceEquals(group.Execution, this))
                    {
                        continue;
                    }

                    try
                    {
                        group.Cancel();
                    }
                    catch (Exception ex)
                    {
                        firstException = firstException ?? ex;
                    }
                }

                cleanupActiveTasks.AddRange(activeTasks);
                foreach (var task in cleanupActiveTasks)
                {
                    if (task == null || task.IsCompleted || !ReferenceEquals(task.OwningExecution, this))
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
                cleanupTaskGroups.Clear();
                cleanupActiveTasks.Clear();
                taskGroups.Clear();
                activeTasks.Clear();
            }

            return firstException;
        }
    }
}
