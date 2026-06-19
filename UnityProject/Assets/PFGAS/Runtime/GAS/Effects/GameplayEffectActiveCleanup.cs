using System.Collections.Generic;

namespace PFGAS.Runtime
{
    internal sealed class GameplayEffectActiveCleanup
    {
        private readonly CombatUnit owner;
        private readonly Dictionary<int, ActiveGameplayEffect> activeEffects;

        public GameplayEffectActiveCleanup(
            CombatUnit owner,
            Dictionary<int, ActiveGameplayEffect> activeEffects)
        {
            this.owner = owner;
            this.activeEffects = activeEffects;
        }

        public void Cleanup(
            ActiveGameplayEffect activeEffect,
            bool removeTags,
            bool removeActiveRecord,
            bool cleanupSubscriptions = true)
        {
            if (cleanupSubscriptions)
            {
                activeEffect.DeactivateTriggers();
                activeEffect.UnsubscribeAll();
            }

            if (activeEffect.ModifierSourceHandle.IsValid)
            {
                owner.Attributes.RemoveModifierSource(activeEffect.ModifierSourceHandle);
                activeEffect.ClearModifierSource();
            }

            if (removeTags)
            {
                owner.Tags.RemoveSourceTags(activeEffect);
            }

            if (removeActiveRecord)
            {
                activeEffects.Remove(activeEffect.Handle.Value);
            }
        }
    }
}
