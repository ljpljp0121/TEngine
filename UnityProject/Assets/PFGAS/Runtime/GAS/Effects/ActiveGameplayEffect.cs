using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    public sealed class ActiveGameplayEffect
    {
        private readonly List<Action> unsubscribeActions = new List<Action>();
        private readonly List<GameplayEffectTriggerSpec> activeTriggers = new List<GameplayEffectTriggerSpec>();

        internal ActiveGameplayEffect(GameplayEffectHandle handle, GameplayEffectSpec spec)
        {
            Handle = handle;
            Spec = spec;
            Duration = spec.Effect.Lifetime.Duration;
            Period = spec.Effect.Lifetime.Period;
            NextPeriodTime = Period > 0f ? Period : 0f;
            StackCount = spec.StackCount;
            ModifierSourceHandle = ModifierSourceHandle.Invalid;
            IsInfinite = spec.Effect.Lifetime.Policy == GameplayEffectDurationPolicy.Infinite;
        }

        public GameplayEffectHandle Handle { get; }

        public GameplayEffectSpec Spec { get; }

        public GameplayEffect Effect => Spec.Effect;

        public CombatUnit Source => Spec.Source;

        public CombatUnit Target => Spec.Target;

        public float ElapsedTime { get; private set; }

        public float Duration { get; }

        public float RemainingTime => IsInfinite ? float.PositiveInfinity : Math.Max(0f, Duration - ElapsedTime);

        public float Period { get; }

        public float NextPeriodTime { get; private set; }

        public int StackCount { get; private set; }

        public ModifierSourceHandle ModifierSourceHandle { get; private set; }

        internal ModifierSource ModifierSource { get; private set; }

        public bool IsInfinite { get; }

        public bool IsExpired { get; private set; }

        internal bool IsRebuildingModifierSource { get; set; }

        internal bool IsModifierSourceDirty { get; private set; }

        internal void MarkModifierSourceDirty()
        {
            IsModifierSourceDirty = true;
        }

        internal void ClearModifierSourceDirty()
        {
            IsModifierSourceDirty = false;
        }

        internal void SetModifierSource(ModifierSourceHandle handle, ModifierSource source)
        {
            ModifierSourceHandle = handle;
            ModifierSource = source;
        }

        internal void ClearModifierSource()
        {
            ModifierSourceHandle = ModifierSourceHandle.Invalid;
            ModifierSource = null;
        }

        internal void SetStackCount(int stackCount)
        {
            GASGuard.Positive(stackCount, nameof(stackCount), "Stack count must be positive.");

            StackCount = stackCount;
            Spec.SetStackCount(stackCount);
        }

        internal void RefreshTiming()
        {
            ElapsedTime = 0f;
            NextPeriodTime = Period > 0f ? Period : 0f;
            IsExpired = false;
        }

        internal void RestoreTiming(float elapsedTime, float nextPeriodTime, bool isExpired)
        {
            ElapsedTime = elapsedTime;
            NextPeriodTime = nextPeriodTime;
            IsExpired = isExpired;
        }

        internal void AdvanceTime(float deltaTime)
        {
            ElapsedTime += deltaTime;
        }

        internal void AdvancePeriod()
        {
            NextPeriodTime += Period;
        }

        internal void AddUnsubscribeAction(Action unsubscribe)
        {
            if (unsubscribe != null)
            {
                unsubscribeActions.Add(unsubscribe);
            }
        }

        internal void UnsubscribeAll()
        {
            for (var i = unsubscribeActions.Count - 1; i >= 0; i--)
            {
                unsubscribeActions[i]();
            }

            unsubscribeActions.Clear();
        }

        internal void AddActiveTrigger(GameplayEffectTriggerSpec trigger)
        {
            activeTriggers.Add(trigger);
        }

        internal void DeactivateTriggers()
        {
            var context = new GameplayEffectTriggerContext(Spec, this);
            for (var i = activeTriggers.Count - 1; i >= 0; i--)
            {
                activeTriggers[i].Trigger.Deactivate(context);
            }

            activeTriggers.Clear();
        }

        internal void MarkExpired()
        {
            IsExpired = true;
        }
    }
}
