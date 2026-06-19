using System;
using System.Collections.Generic;
using System.Linq;

namespace PFGAS.Runtime
{
    /// <summary>Spec 创建时已经捕获的 Snapshot 数值集合。</summary>
    public sealed class GameplayEffectCapturedValues
    {
        private readonly Dictionary<int, float> values = new Dictionary<int, float>();

        public int Count => values.Count;

        public bool TryGetValue(int modifierIndex, out float value)
        {
            return values.TryGetValue(modifierIndex, out value);
        }

        internal void SetValue(int modifierIndex, float value)
        {
            values[modifierIndex] = value;
        }
    }

    /// <summary>一次 GameplayEffect 应用请求的运行时规格，绑定定义、Source、Target 和 Payload。</summary>
    public sealed class GameplayEffectSpec
    {
        internal GameplayEffectSpec(
            GameplayEffect effect,
            CombatUnit source,
            CombatUnit target,
            int level,
            object payload,
            GameplayEffectCapturedValues capturedValues)
        {
            GASGuard.Positive(level, nameof(level), "GameplayEffectSpec level must be positive.");

            Effect = effect;
            Source = source;
            Target = target;
            Level = level;
            Payload = payload;
            StackCount = 1;
            CapturedValues = capturedValues ?? new GameplayEffectCapturedValues();
        }

        public GameplayEffect Effect { get; }

        public CombatUnit Source { get; }

        public CombatUnit Target { get; }

        public int Level { get; }

        public object Payload { get; }

        public int StackCount { get; private set; }

        public GameplayEffectCapturedValues CapturedValues { get; }

        internal void SetStackCount(int stackCount)
        {
            GASGuard.Positive(stackCount, nameof(stackCount), "Stack count must be positive.");

            StackCount = stackCount;
        }
    }

    /// <summary>GameplayEffect Apply 成功后返回的句柄和属性变化快照。</summary>
    public readonly struct GameplayEffectApplyResult
    {
        public GameplayEffectApplyResult(
            GameplayEffectHandle handle,
            IEnumerable<AttributeChange> attributeChanges)
        {
            Handle = handle;
            AttributeChanges = attributeChanges == null
                ? Array.Empty<AttributeChange>()
                : attributeChanges.ToArray();
        }

        public GameplayEffectHandle Handle { get; }

        public IReadOnlyList<AttributeChange> AttributeChanges { get; }
    }
}
