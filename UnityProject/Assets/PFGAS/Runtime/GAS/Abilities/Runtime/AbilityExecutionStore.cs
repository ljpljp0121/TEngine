using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 跟踪并 Tick 当前运行中的 Ability 执行实例。
    /// </summary>
    internal sealed class AbilityExecutionStore
    {
        private readonly List<AbilityExecution> runningExecutions = new List<AbilityExecution>();
        private readonly List<AbilityExecution> cachedRunningExecutions = new List<AbilityExecution>();

        public IReadOnlyList<AbilityExecution> GetRunningExecutionsSnapshot()
        {
            return runningExecutions.ToArray();
        }

        public void TrackIfRunning(AbilityExecution execution)
        {
            if (execution == null || execution.IsCompleted || runningExecutions.Contains(execution))
            {
                return;
            }

            runningExecutions.Add(execution);
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            try
            {
                cachedRunningExecutions.AddRange(runningExecutions);
                foreach (var execution in cachedRunningExecutions)
                {
                    execution.Tick(deltaTime, unscaledDeltaTime);
                }
            }
            finally
            {
                cachedRunningExecutions.Clear();
                RemoveCompletedExecutions();
            }
        }

        public bool CancelByAbilityName(string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName))
            {
                return false;
            }

            return CancelMatching(execution => execution.Spec.Ability.Name == abilityName);
        }

        public void CancelAll()
        {
            CancelMatching(_ => true);
        }

        private bool CancelMatching(Func<AbilityExecution, bool> predicate)
        {
            var cancelled = false;
            Exception cancelException = null;
            try
            {
                cachedRunningExecutions.AddRange(runningExecutions);
                foreach (var execution in cachedRunningExecutions)
                {
                    if (execution == null || !predicate(execution))
                    {
                        continue;
                    }

                    cancelled = true;
                    try
                    {
                        execution.CancelAbility();
                    }
                    catch (Exception ex)
                    {
                        if (cancelException == null)
                        {
                            cancelException = ex;
                        }
                    }
                }
            }
            finally
            {
                cachedRunningExecutions.Clear();
                RemoveCompletedExecutions();
            }

            if (cancelException != null)
            {
                ExceptionDispatchInfo.Capture(cancelException).Throw();
            }

            return cancelled;
        }

        private void RemoveCompletedExecutions()
        {
            for (var i = runningExecutions.Count - 1; i >= 0; i--)
            {
                if (runningExecutions[i].IsCompleted)
                {
                    runningExecutions.RemoveAt(i);
                }
            }
        }
    }
}
