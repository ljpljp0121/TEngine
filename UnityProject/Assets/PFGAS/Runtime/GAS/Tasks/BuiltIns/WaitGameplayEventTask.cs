using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 等待匹配 Gameplay Event 到达后完成的运行时 Task。
    /// </summary>
    public class WaitGameplayEventTask : AbilityTask
    {
        private readonly AbilityExecutionContext context;
        private readonly string eventName;
        private Action<GameplayEvent> handler;
        private bool subscribed;
        private bool gameplayEventCallbackInvoked;

        public WaitGameplayEventTask(AbilityExecutionContext context, string eventName)
        {
            this.context = context;
            this.eventName = eventName ?? string.Empty;
        }

        public string EventName => eventName;

        public bool HasReceivedEvent { get; private set; }

        public GameplayEvent ReceivedEvent { get; private set; }

        public bool HasInvokedGameplayEventCallback => gameplayEventCallbackInvoked;

        public bool IsSubscribed => subscribed;

        public event Action<WaitGameplayEventTask, GameplayEvent> GameplayEventReceived;

        public WaitGameplayEventTask OnGameplayEventReceived(Action<GameplayEvent> callback)
        {
            if (callback != null)
            {
                GameplayEventReceived += (_, gameplayEvent) => callback(gameplayEvent);
            }

            return this;
        }

        public WaitGameplayEventTask OnGameplayEventReceived(Action<WaitGameplayEventTask, GameplayEvent> callback)
        {
            if (callback != null)
            {
                GameplayEventReceived += callback;
            }

            return this;
        }

        public bool TryGetReceivedEvent<TPayload, TTargetData>(
            out GameplayEvent<TPayload, TTargetData> gameplayEvent)
            where TPayload : IGameplayEventPayload
            where TTargetData : IAbilityTargetData
        {
            if (HasReceivedEvent)
            {
                gameplayEvent = ReceivedEvent.As<TPayload, TTargetData>();
                return true;
            }

            gameplayEvent = default;
            return false;
        }

        protected override void OnStart()
        {
            if (string.IsNullOrEmpty(eventName) || context.Owner == null || context.Owner.GameplayEventBus == null)
            {
                Complete(false);
                return;
            }

            handler = OnGameplayEvent;
            context.Owner.GameplayEventBus.Subscribe(eventName, handler);
            subscribed = true;
        }

        protected override void OnCancel()
        {
            Unsubscribe();
        }

        protected override void OnComplete(bool succeeded)
        {
            try
            {
                if (succeeded)
                {
                    InvokeGameplayEventCallbackOnce();
                }
            }
            finally
            {
                Unsubscribe();
            }
        }

        private void OnGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (IsCompleted)
            {
                return;
            }

            ReceivedEvent = gameplayEvent;
            HasReceivedEvent = true;
            Complete(true);
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            context.Owner.GameplayEventBus.Unsubscribe(eventName, handler);
            subscribed = false;
            handler = null;
        }

        private void InvokeGameplayEventCallbackOnce()
        {
            if (gameplayEventCallbackInvoked || !HasReceivedEvent)
            {
                return;
            }

            gameplayEventCallbackInvoked = true;
            GameplayEventReceived?.Invoke(this, ReceivedEvent);
        }
    }

    /// <summary>
    /// 暴露强类型 Gameplay Event 数据的等待事件 Task。
    /// </summary>
    public sealed class WaitGameplayEventTask<TPayload, TTargetData> : WaitGameplayEventTask
        where TPayload : IGameplayEventPayload
        where TTargetData : IAbilityTargetData
    {
        private bool typedBridgeRegistered;

        public WaitGameplayEventTask(AbilityExecutionContext context, string eventName)
            : base(context, eventName)
        {
        }

        public event Action<WaitGameplayEventTask<TPayload, TTargetData>, GameplayEvent<TPayload, TTargetData>>
            TypedGameplayEventReceived;

        public WaitGameplayEventTask<TPayload, TTargetData> OnGameplayEventReceived(
            Action<GameplayEvent<TPayload, TTargetData>> callback)
        {
            if (callback != null)
            {
                TypedGameplayEventReceived += (_, gameplayEvent) => callback(gameplayEvent);
                RegisterTypedBridge();
            }

            return this;
        }

        public WaitGameplayEventTask<TPayload, TTargetData> OnGameplayEventReceived(
            Action<WaitGameplayEventTask<TPayload, TTargetData>, GameplayEvent<TPayload, TTargetData>> callback)
        {
            if (callback != null)
            {
                TypedGameplayEventReceived += callback;
                RegisterTypedBridge();
            }

            return this;
        }

        private void RegisterTypedBridge()
        {
            if (typedBridgeRegistered)
            {
                return;
            }

            typedBridgeRegistered = true;
            base.OnGameplayEventReceived(InvokeTypedCallbacks);
        }

        private void InvokeTypedCallbacks(GameplayEvent gameplayEvent)
        {
            TypedGameplayEventReceived?.Invoke(this, gameplayEvent.As<TPayload, TTargetData>());
        }
    }
}
