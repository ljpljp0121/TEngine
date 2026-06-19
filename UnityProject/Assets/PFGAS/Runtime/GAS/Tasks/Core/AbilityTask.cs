using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// Ability 激活后创建的运行时 Task 实例。
    /// </summary>
    public abstract class AbilityTask
    {
        private bool ownerNotificationSent;
        private bool callbackNotificationSent;

        public bool IsStarted { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool Succeeded { get; private set; }
        public bool WasCancelled { get; private set; }
        public bool HasNotifiedOwner => ownerNotificationSent;
        public bool HasInvokedCallbacks => callbackNotificationSent;

        public AbilityExecution OwningExecution { get; private set; }

        public AbilityExecutionContext Context { get; private set; }

        public virtual bool RequiresTick => false;

        public event Action<AbilityTask> Completed;

        public event Action<AbilityTask> Failed;

        public event Action<AbilityTask> Cancelled;

        public event Action<AbilityTask, bool> Finished;

        internal void AttachToExecution(AbilityExecution execution, AbilityExecutionContext context)
        {
            if (OwningExecution != null && !ReferenceEquals(OwningExecution, execution))
            {
                throw new InvalidOperationException("AbilityTask is already owned by another AbilityExecution.");
            }

            OwningExecution = execution;
            Context = context ?? execution.Context;
        }

        public void Start()
        {
            if (IsStarted || IsCompleted)
            {
                return;
            }

            IsStarted = true;
            OnStart();
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (!IsStarted || IsCompleted || !RequiresTick)
            {
                return;
            }

            OnTick(deltaTime, unscaledDeltaTime);
        }

        public void Cancel()
        {
            if (IsCompleted)
            {
                return;
            }

            WasCancelled = true;
            try
            {
                OnCancel();
            }
            finally
            {
                Complete(false);
            }
        }

        protected void Complete(bool succeeded)
        {
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
            Succeeded = succeeded;
            try
            {
                OnComplete(succeeded);
            }
            finally
            {
                try
                {
                    InvokeCallbacksOnce();
                }
                finally
                {
                    NotifyOwnerOnce();
                }
            }
        }

        private void InvokeCallbacksOnce()
        {
            if (callbackNotificationSent)
            {
                return;
            }

            callbackNotificationSent = true;
            if (WasCancelled)
            {
                Cancelled?.Invoke(this);
            }
            else if (Succeeded)
            {
                Completed?.Invoke(this);
            }
            else
            {
                Failed?.Invoke(this);
            }

            Finished?.Invoke(this, Succeeded);
        }

        private void NotifyOwnerOnce()
        {
            if (ownerNotificationSent)
            {
                return;
            }

            ownerNotificationSent = true;
            if (WasCancelled)
            {
                OwningExecution?.NotifyTaskCancelled(this);
            }
            else
            {
                OwningExecution?.NotifyTaskCompleted(this);
            }
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnTick(float deltaTime, float unscaledDeltaTime)
        {
        }

        protected virtual void OnCancel()
        {
        }

        protected virtual void OnComplete(bool succeeded)
        {
        }
    }
}
