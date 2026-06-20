using System;
using System.Collections.Generic;
using System.Linq;

namespace PFGAS.Runtime
{
    /// <summary>不可变 GameplayEffect 定义，集中描述生命周期、Modifier、Tag、Stacking、Execution 和 Trigger。</summary>
    public sealed class GameplayEffect
    {
        public GameplayEffect(
            string effectId,
            GameplayEffectLifetime lifetime,
            IEnumerable<GameplayEffectModifierSpec> modifiers = null,
            GameplayEffectTagRequirements tags = default,
            GameplayEffectStackingPolicy stacking = default,
            IEnumerable<PFTagId> grantedTags = null,
            IEnumerable<GameplayEffectExecutionSpec> executions = null,
            IEnumerable<GameplayEffectTriggerSpec> triggers = null)
        {
            EffectId = NormalizeEffectId(effectId);
            Lifetime = lifetime;
            Tags = tags.Normalized();
            Stacking = stacking.Normalized();
            GrantedTags = CopyOrEmpty(grantedTags);
            Modifiers = CopyOrEmpty(modifiers);
            Executions = CopyOrEmpty(executions);
            Triggers = CopyOrEmpty(triggers);
        }

        public string EffectId { get; }

        public GameplayEffectLifetime Lifetime { get; }

        public GameplayEffectTagRequirements Tags { get; }

        public GameplayEffectStackingPolicy Stacking { get; }

        public IReadOnlyList<PFTagId> GrantedTags { get; }

        public IReadOnlyList<GameplayEffectModifierSpec> Modifiers { get; }

        public IReadOnlyList<GameplayEffectExecutionSpec> Executions { get; }

        public IReadOnlyList<GameplayEffectTriggerSpec> Triggers { get; }

        private static T[] CopyOrEmpty<T>(IEnumerable<T> values)
        {
            if (values == null)
            {
                return Array.Empty<T>();
            }

            return values.ToArray();
        }

        private static string NormalizeEffectId(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                GASGuard.ThrowArgument("GameplayEffect EffectId cannot be empty.", nameof(effectId));
            }

            return effectId.Trim();
        }
    }
}
