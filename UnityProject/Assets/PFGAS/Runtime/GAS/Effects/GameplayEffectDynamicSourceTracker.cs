using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    internal sealed class GameplayEffectDynamicSourceTracker
    {
        private readonly CombatUnit owner;
        private readonly GameplayEffectModifierResolver modifierResolver;
        private readonly List<ActiveGameplayEffect> dirtyEffects = new List<ActiveGameplayEffect>();
        private readonly List<ActiveGameplayEffect> dirtySnapshot = new List<ActiveGameplayEffect>();
        private readonly HashSet<int> dirtyHandles = new HashSet<int>();

        public GameplayEffectDynamicSourceTracker(
            CombatUnit owner,
            GameplayEffectModifierResolver modifierResolver)
        {
            this.owner = owner;
            this.modifierResolver = modifierResolver;
        }

        public void Register(
            ActiveGameplayEffect activeEffect,
            Func<ActiveGameplayEffect, bool> isActive)
        {
            if (activeEffect.Source == null || activeEffect.Source.Attributes == null)
            {
                return;
            }

            var dependencies = new HashSet<PFAttributeId>();
            var modifiers = activeEffect.Effect.Modifiers;
            for (var i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier.Phase != GameplayEffectModifierPhase.Ongoing ||
                    modifier.CapturePolicy != GameplayEffectCapturePolicy.DynamicWhileActive ||
                    !modifier.Magnitude.RequiresSource)
                {
                    continue;
                }

                foreach (var dependency in modifier.Magnitude.SourceDependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            if (dependencies.Count == 0)
            {
                return;
            }

            Action<AttributeChange> handler = change =>
            {
                if (!dependencies.Contains(change.AttributeId) || !isActive(activeEffect))
                {
                    return;
                }

                MarkDirty(activeEffect, isActive);
            };

            activeEffect.Source.Attributes.AttributeChanged += handler;
            activeEffect.AddUnsubscribeAction(() => activeEffect.Source.Attributes.AttributeChanged -= handler);
        }

        public void Flush(Func<ActiveGameplayEffect, bool> isActive)
        {
            if (dirtyEffects.Count == 0)
            {
                return;
            }

            dirtySnapshot.Clear();
            dirtySnapshot.AddRange(dirtyEffects);
            dirtyEffects.Clear();
            dirtyHandles.Clear();

            for (var i = 0; i < dirtySnapshot.Count; i++)
            {
                var activeEffect = dirtySnapshot[i];
                if (!isActive(activeEffect) || !activeEffect.IsModifierSourceDirty)
                {
                    continue;
                }

                var rebuildResult = RebuildOngoingModifierSource(activeEffect);
                if (rebuildResult.Failed)
                {
                    GASGuard.ThrowInvalidOperation(rebuildResult.Failure);
                }
            }

            dirtySnapshot.Clear();
        }

        public GASResult RebuildOngoingModifierSource(ActiveGameplayEffect activeEffect)
        {
            if (activeEffect.IsRebuildingModifierSource)
            {
                return GASResult.Success();
            }

            activeEffect.IsRebuildingModifierSource = true;
            try
            {
                var newSourceResult = modifierResolver.CreateOngoingModifierSource(
                    activeEffect.Spec,
                    activeEffect.StackCount);
                if (newSourceResult.Failed)
                {
                    return GASResult.Fail(newSourceResult.Failure);
                }

                var replaceResult = ReplaceOngoingModifierSource(activeEffect, newSourceResult.Value);
                if (replaceResult.Failed)
                {
                    return replaceResult;
                }

                activeEffect.ClearModifierSourceDirty();
                return GASResult.Success();
            }
            finally
            {
                activeEffect.IsRebuildingModifierSource = false;
            }
        }

        public GASResult ReplaceOngoingModifierSource(
            ActiveGameplayEffect activeEffect,
            ModifierSource newSource)
        {
            var oldHandle = activeEffect.ModifierSourceHandle;
            var oldSource = activeEffect.ModifierSource;
            ModifierSourceHandle newHandle = ModifierSourceHandle.Invalid;

            try
            {
                using (owner.Attributes.BatchUpdate())
                {
                    if (oldHandle.IsValid)
                    {
                        owner.Attributes.RemoveModifierSource(oldHandle);
                        activeEffect.ClearModifierSource();
                    }

                    if (newSource != null)
                    {
                        newHandle = owner.Attributes.AddModifierSource(newSource);
                        activeEffect.SetModifierSource(newHandle, newSource);
                    }
                }

                return GASResult.Success();
            }
            catch (Exception exception)
            {
                if (newHandle.IsValid)
                {
                    owner.Attributes.RemoveModifierSource(newHandle);
                }

                activeEffect.ClearModifierSource();
                if (oldHandle.IsValid && oldSource != null && !activeEffect.ModifierSourceHandle.IsValid)
                {
                    var restoredHandle = owner.Attributes.AddModifierSource(oldSource);
                    activeEffect.SetModifierSource(restoredHandle, oldSource);
                }

                return GASResult.Fail("DynamicRebuildFailed", exception.Message);
            }
        }

        private void MarkDirty(
            ActiveGameplayEffect activeEffect,
            Func<ActiveGameplayEffect, bool> isActive)
        {
            if (!isActive(activeEffect))
            {
                return;
            }

            activeEffect.MarkModifierSourceDirty();
            if (dirtyHandles.Add(activeEffect.Handle.Value))
            {
                dirtyEffects.Add(activeEffect);
            }
        }
    }
}
