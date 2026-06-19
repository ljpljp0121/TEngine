using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 分发 Gameplay Event。
    /// </summary>
    public sealed class GameplayEventBus
    {
        private readonly EventStation<string> eventStation = new EventStation<string>();
        private readonly System.Collections.Generic.Dictionary<TypedSubscriptionKey, Action<GameplayEvent>> typedHandlers =
            new System.Collections.Generic.Dictionary<TypedSubscriptionKey, Action<GameplayEvent>>();

        public void Subscribe(string eventName, Action<GameplayEvent> handler)
        {
            if (handler == null) return;
            eventStation.Subscribe(eventName, handler);
        }

        public void Subscribe<TPayload, TTargetData>(
            string eventName,
            Action<GameplayEvent<TPayload, TTargetData>> handler)
            where TPayload : IGameplayEventPayload
            where TTargetData : IAbilityTargetData
        {
            if (handler == null) return;

            var key = new TypedSubscriptionKey(eventName, handler, typeof(TPayload), typeof(TTargetData));
            if (typedHandlers.ContainsKey(key))
            {
                return;
            }

            Action<GameplayEvent> wrappedHandler = gameplayEvent =>
                handler(gameplayEvent.As<TPayload, TTargetData>());
            typedHandlers.Add(key, wrappedHandler);
            eventStation.Subscribe(eventName, wrappedHandler);
        }

        public void Unsubscribe(string eventName, Action<GameplayEvent> handler)
        {
            if (handler == null) return;
            eventStation.Unsubscribe(eventName, handler);
        }

        public void Unsubscribe<TPayload, TTargetData>(
            string eventName,
            Action<GameplayEvent<TPayload, TTargetData>> handler)
            where TPayload : IGameplayEventPayload
            where TTargetData : IAbilityTargetData
        {
            if (handler == null) return;

            var key = new TypedSubscriptionKey(eventName, handler, typeof(TPayload), typeof(TTargetData));
            if (!typedHandlers.TryGetValue(key, out var wrappedHandler))
            {
                return;
            }

            typedHandlers.Remove(key);
            eventStation.Unsubscribe(eventName, wrappedHandler);
        }

        public void Publish(GameplayEvent gameplayEvent)
        {
            if (string.IsNullOrEmpty(gameplayEvent.EventName)) return;
            eventStation.Publish(gameplayEvent.EventName, gameplayEvent);
        }

        public void Publish<TPayload, TTargetData>(GameplayEvent<TPayload, TTargetData> gameplayEvent)
            where TPayload : IGameplayEventPayload
            where TTargetData : IAbilityTargetData
        {
            Publish(gameplayEvent.Untyped);
        }

        public void Publish(
            string eventName,
            CombatUnit source,
            CombatUnit target = null)
        {
            Publish(new GameplayEvent(eventName, source, target));
        }

        public void Publish<TPayload>(
            string eventName,
            CombatUnit source,
            CombatUnit target,
            TPayload payload)
            where TPayload : IGameplayEventPayload
        {
            Publish(
                eventName,
                source,
                target,
                payload,
                EmptyAbilityTargetData.Value);
        }

        public void Publish<TPayload, TTargetData>(
            string eventName,
            CombatUnit source,
            CombatUnit target,
            TPayload payload,
            TTargetData targetData)
            where TPayload : IGameplayEventPayload
            where TTargetData : IAbilityTargetData
        {
            Publish(new GameplayEvent<TPayload, TTargetData>(
                eventName,
                source,
                target,
                payload,
                targetData));
        }

        public bool HasEvent(string eventName)
        {
            return !string.IsNullOrEmpty(eventName) && eventStation.HasEvent(eventName);
        }

        public void Clear()
        {
            typedHandlers.Clear();
            eventStation.UnregisterAll();
        }

        /// <summary>
        /// 标识一个强类型 Gameplay Event 订阅的键。
        /// </summary>
        private readonly struct TypedSubscriptionKey : IEquatable<TypedSubscriptionKey>
        {
            private readonly string eventName;
            private readonly Delegate handler;
            private readonly Type payloadType;
            private readonly Type targetDataType;

            public TypedSubscriptionKey(
                string eventName,
                Delegate handler,
                Type payloadType,
                Type targetDataType)
            {
                this.eventName = eventName ?? string.Empty;
                this.handler = handler;
                this.payloadType = payloadType;
                this.targetDataType = targetDataType;
            }

            public bool Equals(TypedSubscriptionKey other)
            {
                return string.Equals(eventName, other.eventName, StringComparison.Ordinal) &&
                       Equals(handler, other.handler) &&
                       payloadType == other.payloadType &&
                       targetDataType == other.targetDataType;
            }

            public override bool Equals(object obj)
            {
                return obj is TypedSubscriptionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = eventName != null ? eventName.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (handler != null ? handler.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (payloadType != null ? payloadType.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (targetDataType != null ? targetDataType.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
