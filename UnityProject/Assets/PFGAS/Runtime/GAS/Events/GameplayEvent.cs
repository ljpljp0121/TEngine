using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// Gameplay Event 的运行时标识和类型元数据。
    /// </summary>
    public readonly struct GameplayEvent
    {
        private readonly object payload;
        private readonly object targetData;

        public GameplayEvent(
            string eventName,
            CombatUnit source,
            CombatUnit target = null)
            : this(
                eventName,
                source,
                target,
                EmptyGameplayEventPayload.Value,
                EmptyAbilityTargetData.Value)
        {
        }

        internal GameplayEvent(
            string eventName,
            CombatUnit source,
            CombatUnit target,
            object payload,
            object targetData)
        {
            EventName = eventName ?? string.Empty;
            Source = source;
            Target = target;
            this.payload = payload;
            this.targetData = targetData;
            PayloadType = payload?.GetType() ?? typeof(EmptyGameplayEventPayload);
            TargetDataType = targetData?.GetType() ?? typeof(EmptyAbilityTargetData);
        }

        public string EventName { get; }

        public CombatUnit Source { get; }

        public CombatUnit Target { get; }

        public Type PayloadType { get; }

        public Type TargetDataType { get; }

        public bool HasPayload => PayloadType != typeof(EmptyGameplayEventPayload);

        public bool HasTargetData => TargetDataType != typeof(EmptyAbilityTargetData);

        public bool IsMatch(string eventName)
        {
            return string.Equals(EventName, eventName, StringComparison.Ordinal);
        }

        public bool TryGetPayload<TPayload>(out TPayload value)
            where TPayload : IGameplayEventPayload
        {
            if (payload is TPayload typedPayload)
            {
                value = typedPayload;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetTargetData<TTargetData>(out TTargetData value)
            where TTargetData : IAbilityTargetData
        {
            if (targetData is TTargetData typedTargetData)
            {
                value = typedTargetData;
                return true;
            }

            value = default;
            return false;
        }

        public GameplayEvent<TPayload, TTargetData> As<TPayload, TTargetData>()
            where TPayload : IGameplayEventPayload
            where TTargetData : IAbilityTargetData
        {
            if (!TryGetPayload<TPayload>(out var typedPayload))
            {
                throw new InvalidOperationException(
                    $"GameplayEvent '{EventName}' payload type '{PayloadType.Name}' cannot be used as '{typeof(TPayload).Name}'.");
            }

            if (!TryGetTargetData<TTargetData>(out var typedTargetData))
            {
                throw new InvalidOperationException(
                    $"GameplayEvent '{EventName}' target data type '{TargetDataType.Name}' cannot be used as '{typeof(TTargetData).Name}'.");
            }

            return new GameplayEvent<TPayload, TTargetData>(
                EventName,
                Source,
                Target,
                typedPayload,
                typedTargetData);
        }
    }

    /// <summary>
    /// 通过 Ability Task 传递的强类型 Gameplay Event 数据。
    /// </summary>
    public readonly struct GameplayEvent<TPayload, TTargetData>
        where TPayload : IGameplayEventPayload
        where TTargetData : IAbilityTargetData
    {
        public GameplayEvent(
            string eventName,
            CombatUnit source,
            CombatUnit target,
            TPayload payload,
            TTargetData targetData)
        {
            EventName = eventName ?? string.Empty;
            Source = source;
            Target = target;
            Payload = payload;
            TargetData = targetData;
        }

        public string EventName { get; }

        public CombatUnit Source { get; }

        public CombatUnit Target { get; }

        public TPayload Payload { get; }

        public TTargetData TargetData { get; }

        public GameplayEvent Untyped => new GameplayEvent(EventName, Source, Target, Payload, TargetData);

        public bool IsMatch(string eventName)
        {
            return string.Equals(EventName, eventName, StringComparison.Ordinal);
        }
    }
}
