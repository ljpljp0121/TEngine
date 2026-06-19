using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    public sealed class GameplayEffectContainer
    {
        private readonly Dictionary<int, ActiveGameplayEffect> activeEffects =
            new Dictionary<int, ActiveGameplayEffect>();
        private readonly List<ActiveGameplayEffect> activeSnapshot =
            new List<ActiveGameplayEffect>();
        private readonly List<AttributeModifier> reusableBaseModifiers =
            new List<AttributeModifier>();
        private readonly List<AttributeChange> reusableApplyChanges =
            new List<AttributeChange>();
        private readonly GameplayEffectModifierResolver modifierResolver;
        private readonly GameplayEffectStackingResolver stackingResolver =
            new GameplayEffectStackingResolver();
        private readonly GameplayEffectActiveCleanup activeCleanup;
        private readonly GameplayEffectDynamicSourceTracker dynamicSourceTracker;

        private int nextHandleId;

        public GameplayEffectContainer(CombatUnit owner)
        {
            Owner = owner;
            modifierResolver = new GameplayEffectModifierResolver(Owner);
            activeCleanup = new GameplayEffectActiveCleanup(Owner, activeEffects);
            dynamicSourceTracker = new GameplayEffectDynamicSourceTracker(Owner, modifierResolver);
        }

        public CombatUnit Owner { get; }

        public int ActiveEffectCount => activeEffects.Count;

        public GASResult<GameplayEffectSpec> CreateSpec(
            GameplayEffect effect,
            CombatUnit source,
            CombatUnit target,
            int level = 1,
            object payload = null)
        {
            var contextValidation = GameplayEffectValidator.ValidateContext(Owner, target);
            if (contextValidation.Failed)
            {
                return GASResult<GameplayEffectSpec>.Fail(contextValidation.Failure);
            }

            GameplayEffectValidator.ValidateEffectConfiguration(effect);

            var tagValidation = GameplayEffectValidator.ValidateTags(effect, source, target);
            if (tagValidation.Failed)
            {
                return GASResult<GameplayEffectSpec>.Fail(tagValidation.Failure);
            }

            var capturedValues = new GameplayEffectCapturedValues();
            var captureResult = modifierResolver.CaptureSnapshotValues(effect, source, target, capturedValues);
            if (captureResult.Failed)
            {
                return GASResult<GameplayEffectSpec>.Fail(captureResult.Failure);
            }

            return GASResult<GameplayEffectSpec>.Success(
                new GameplayEffectSpec(effect, source, target, level, payload, capturedValues));
        }

        /// <summary>正式便利入口：为当前 Owner 创建 spec，并委托到 spec-based Apply 主路径。</summary>
        public GASResult<GameplayEffectApplyResult> ApplyToSelf(
            GameplayEffect effect,
            CombatUnit source = null,
            int level = 1,
            object payload = null)
        {
            var specResult = CreateSpec(effect, source, Owner, level, payload);
            if (specResult.Failed)
            {
                return GASResult<GameplayEffectApplyResult>.Fail(specResult.Failure);
            }

            return Apply(specResult.Value);
        }

        public GASResult<GameplayEffectApplyResult> Apply(GameplayEffectSpec spec)
        {
            var planResult = PrepareApply(spec);
            if (planResult.Failed)
            {
                return GASResult<GameplayEffectApplyResult>.Fail(planResult.Failure);
            }

            return CommitApply(planResult.Value);
        }

        public GASResult Remove(GameplayEffectHandle handle)
        {
            if (!IsHandleOwnedByThisContainer(handle))
            {
                return GASResult.Fail("InvalidEffectHandle", "GameplayEffectHandle does not belong to this container.");
            }

            if (!activeEffects.TryGetValue(handle.Value, out var activeEffect))
            {
                return GASResult.Fail("InactiveEffectHandle", "GameplayEffectHandle is not active.");
            }

            return RemoveActiveEffect(activeEffect, true);
        }

        public void RemoveAll()
        {
            activeSnapshot.Clear();
            activeSnapshot.AddRange(activeEffects.Values);
            for (var i = 0; i < activeSnapshot.Count; i++)
            {
                if (activeEffects.ContainsKey(activeSnapshot[i].Handle.Value))
                {
                    RemoveActiveEffect(activeSnapshot[i], true);
                }
            }

            activeSnapshot.Clear();
        }

        public void Tick(float deltaTime)
        {
            GASGuard.Finite(deltaTime, nameof(deltaTime), "Effect tick deltaTime must be finite.");
            GASGuard.NonNegative(deltaTime, nameof(deltaTime), "Effect tick deltaTime cannot be negative.");

            dynamicSourceTracker.Flush(IsActiveEffect);

            if (deltaTime <= 0f || activeEffects.Count == 0)
            {
                return;
            }

            activeSnapshot.Clear();
            activeSnapshot.AddRange(activeEffects.Values);

            for (var i = 0; i < activeSnapshot.Count; i++)
            {
                var activeEffect = activeSnapshot[i];
                if (!IsActiveEffect(activeEffect))
                {
                    continue;
                }

                activeEffect.AdvanceTime(deltaTime);
                TickPeriodic(activeEffect);

                if (!activeEffect.IsInfinite &&
                    activeEffect.ElapsedTime + PFGASHelper.ValueEpsilon >= activeEffect.Duration)
                {
                    var removeResult = RemoveActiveEffect(activeEffect, true);
                    if (removeResult.Failed)
                    {
                        GASGuard.ThrowInvalidOperation(removeResult.Failure);
                    }
                }
            }

            activeSnapshot.Clear();
        }

        public bool TryGetActiveEffect(
            GameplayEffectHandle handle,
            out ActiveGameplayEffect effect)
        {
            if (!IsHandleOwnedByThisContainer(handle))
            {
                effect = null;
                return false;
            }

            return activeEffects.TryGetValue(handle.Value, out effect);
        }

        private GASResult<ApplyPlan> PrepareApply(GameplayEffectSpec spec)
        {
            var contextValidation = GameplayEffectValidator.ValidateContext(Owner, spec.Target);
            if (contextValidation.Failed)
            {
                return GASResult<ApplyPlan>.Fail(contextValidation.Failure);
            }

            GameplayEffectValidator.ValidateEffectConfiguration(spec.Effect);

            var tagValidation = GameplayEffectValidator.ValidateTags(spec.Effect, spec.Source, spec.Target);
            if (tagValidation.Failed)
            {
                return GASResult<ApplyPlan>.Fail(tagValidation.Failure);
            }

            var stackingDecision = GameplayEffectStackingDecision.CreateNew();
            if (spec.Effect.Lifetime.Policy != GameplayEffectDurationPolicy.Instant)
            {
                var stackingResult = stackingResolver.Decide(spec, activeEffects.Values);
                if (stackingResult.Failed)
                {
                    return GASResult<ApplyPlan>.Fail(stackingResult.Failure);
                }

                stackingDecision = stackingResult.Value;
            }

            if (stackingDecision.Action == GameplayEffectStackingAction.ReturnExisting ||
                stackingDecision.Action == GameplayEffectStackingAction.RefreshExisting)
            {
                return GASResult<ApplyPlan>.Success(
                    new ApplyPlan(
                        spec,
                        stackingDecision,
                        Array.Empty<AttributeModifier>(),
                        null,
                        Array.Empty<AttributeModifier>()));
            }

            if (stackingDecision.Action == GameplayEffectStackingAction.StackExisting)
            {
                var stackSourceResult = modifierResolver.CreateOngoingModifierSource(
                    stackingDecision.ExistingEffect.Spec,
                    stackingDecision.NewStackCount);
                if (stackSourceResult.Failed)
                {
                    return GASResult<ApplyPlan>.Fail(stackSourceResult.Failure);
                }

                return GASResult<ApplyPlan>.Success(
                    new ApplyPlan(
                        spec,
                        stackingDecision,
                        Array.Empty<AttributeModifier>(),
                        stackSourceResult.Value,
                        Array.Empty<AttributeModifier>()));
            }

            var instantResult = modifierResolver.ResolveModifiers(
                spec,
                GameplayEffectModifierPhase.Instant,
                spec.StackCount);
            if (instantResult.Failed)
            {
                return GASResult<ApplyPlan>.Fail(instantResult.Failure);
            }

            if (spec.Effect.Lifetime.Policy == GameplayEffectDurationPolicy.Instant)
            {
                return GASResult<ApplyPlan>.Success(
                    new ApplyPlan(
                        spec,
                        stackingDecision,
                        instantResult.Value,
                        null,
                        Array.Empty<AttributeModifier>()));
            }

            var ongoingSourceResult = modifierResolver.CreateOngoingModifierSource(spec, spec.StackCount);
            if (ongoingSourceResult.Failed)
            {
                return GASResult<ApplyPlan>.Fail(ongoingSourceResult.Failure);
            }

            var initialPeriodicModifiers = Array.Empty<AttributeModifier>();
            if (spec.Effect.Lifetime.ExecutePeriodicOnApply)
            {
                var initialPeriodicResult = modifierResolver.ResolveModifiers(
                    spec,
                    GameplayEffectModifierPhase.Periodic,
                    spec.StackCount);
                if (initialPeriodicResult.Failed)
                {
                    return GASResult<ApplyPlan>.Fail(initialPeriodicResult.Failure);
                }

                initialPeriodicModifiers = initialPeriodicResult.Value;
            }

            return GASResult<ApplyPlan>.Success(
                new ApplyPlan(
                    spec,
                    stackingDecision,
                    instantResult.Value,
                    ongoingSourceResult.Value,
                    initialPeriodicModifiers));
        }

        private GASResult<GameplayEffectApplyResult> CommitApply(ApplyPlan plan)
        {
            switch (plan.StackingDecision.Action)
            {
                case GameplayEffectStackingAction.ReturnExisting:
                    return GASResult<GameplayEffectApplyResult>.Success(
                        new GameplayEffectApplyResult(
                            plan.StackingDecision.ReturnHandle,
                            Array.Empty<AttributeChange>()));
                case GameplayEffectStackingAction.RefreshExisting:
                    plan.StackingDecision.ExistingEffect.RefreshTiming();
                    return GASResult<GameplayEffectApplyResult>.Success(
                        new GameplayEffectApplyResult(
                            plan.StackingDecision.ReturnHandle,
                            Array.Empty<AttributeChange>()));
                case GameplayEffectStackingAction.StackExisting:
                    return CommitExistingStackApply(plan);
                case GameplayEffectStackingAction.CreateNew:
                    return CommitNewApply(plan);
            }

            return CommitNewApply(plan);
        }

        private GASResult<GameplayEffectApplyResult> CommitExistingStackApply(ApplyPlan plan)
        {
            var decision = plan.StackingDecision;
            var activeEffect = decision.ExistingEffect;
            var oldStackCount = activeEffect.StackCount;
            var oldElapsedTime = activeEffect.ElapsedTime;
            var oldNextPeriodTime = activeEffect.NextPeriodTime;
            var oldIsExpired = activeEffect.IsExpired;

            var onApplyResult = ExecuteExecutions(
                plan.Spec,
                activeEffect,
                GameplayEffectExecutionPhase.OnApply);
            if (onApplyResult.Failed)
            {
                return GASResult<GameplayEffectApplyResult>.Fail(onApplyResult.Failure);
            }

            activeEffect.SetStackCount(decision.NewStackCount);
            if (decision.RefreshTiming)
            {
                activeEffect.RefreshTiming();
            }

            var sourceResult = dynamicSourceTracker.ReplaceOngoingModifierSource(
                activeEffect,
                plan.OngoingSource);
            if (sourceResult.Failed)
            {
                activeEffect.SetStackCount(oldStackCount);
                activeEffect.RestoreTiming(oldElapsedTime, oldNextPeriodTime, oldIsExpired);
                return GASResult<GameplayEffectApplyResult>.Fail(sourceResult.Failure);
            }

            activeEffect.ClearModifierSourceDirty();
            return GASResult<GameplayEffectApplyResult>.Success(
                new GameplayEffectApplyResult(decision.ReturnHandle, Array.Empty<AttributeChange>()));
        }

        private GASResult<GameplayEffectApplyResult> CommitNewApply(ApplyPlan plan)
        {
            var spec = plan.Spec;
            var onApplyResult = ExecuteExecutions(spec, null, GameplayEffectExecutionPhase.OnApply);
            if (onApplyResult.Failed)
            {
                return GASResult<GameplayEffectApplyResult>.Fail(onApplyResult.Failure);
            }

            if (spec.Effect.Lifetime.Policy == GameplayEffectDurationPolicy.Instant)
            {
                var instantChanges = ApplyBaseModifiers(plan.InstantModifiers);
                return GASResult<GameplayEffectApplyResult>.Success(
                    new GameplayEffectApplyResult(GameplayEffectHandle.Invalid, instantChanges));
            }

            var handle = new GameplayEffectHandle(this, ++nextHandleId);
            var activeEffect = new ActiveGameplayEffect(handle, spec);
            var transaction = new GameplayEffectApplyTransaction();

            reusableBaseModifiers.Clear();
            reusableApplyChanges.Clear();

            try
            {
                if (plan.OngoingSource != null)
                {
                    var sourceHandle = Owner.Attributes.AddModifierSource(plan.OngoingSource);
                    activeEffect.SetModifierSource(sourceHandle, plan.OngoingSource);
                    transaction.AddRollback(() =>
                    {
                        if (activeEffect.ModifierSourceHandle.IsValid)
                        {
                            Owner.Attributes.RemoveModifierSource(activeEffect.ModifierSourceHandle);
                            activeEffect.ClearModifierSource();
                        }
                    });
                }

                if (spec.Effect.GrantedTags.Count > 0)
                {
                    Owner.Tags.AddSourceTags(activeEffect, ToTagArray(spec.Effect.GrantedTags));
                    transaction.AddRollback(() => Owner.Tags.RemoveSourceTags(activeEffect));
                }

                activeEffects.Add(handle.Value, activeEffect);
                transaction.AddRollback(() => activeEffects.Remove(handle.Value));

                dynamicSourceTracker.Register(activeEffect, IsActiveEffect);
                transaction.AddRollback(activeEffect.UnsubscribeAll);
                transaction.AddRollback(activeEffect.DeactivateTriggers);

                var triggerResult = ActivateTriggers(activeEffect);
                if (triggerResult.Failed)
                {
                    transaction.Rollback();
                    return GASResult<GameplayEffectApplyResult>.Fail(triggerResult.Failure);
                }

                if (spec.Effect.Lifetime.ExecutePeriodicOnApply)
                {
                    var periodExecutionResult = ExecuteExecutions(
                        spec,
                        activeEffect,
                        GameplayEffectExecutionPhase.OnPeriod);
                    if (periodExecutionResult.Failed)
                    {
                        transaction.Rollback();
                        return GASResult<GameplayEffectApplyResult>.Fail(periodExecutionResult.Failure);
                    }
                }

                if (plan.StackingDecision.ExistingToReplace != null &&
                    activeEffects.ContainsKey(plan.StackingDecision.ExistingToReplace.Handle.Value))
                {
                    var removeResult = RemoveActiveEffect(plan.StackingDecision.ExistingToReplace, true);
                    if (removeResult.Failed)
                    {
                        transaction.Rollback();
                        return GASResult<GameplayEffectApplyResult>.Fail(removeResult.Failure);
                    }
                }

                AppendModifiers(plan.InstantModifiers, reusableBaseModifiers);
                AppendModifiers(plan.InitialPeriodicModifiers, reusableBaseModifiers);
                AppendChanges(ApplyBaseModifiers(reusableBaseModifiers), reusableApplyChanges);

                transaction.Commit();
                return GASResult<GameplayEffectApplyResult>.Success(
                    new GameplayEffectApplyResult(handle, reusableApplyChanges));
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                reusableBaseModifiers.Clear();
                reusableApplyChanges.Clear();
            }
        }

        private void TickPeriodic(ActiveGameplayEffect activeEffect)
        {
            if (activeEffect.Period <= 0f)
            {
                return;
            }

            var tickUntil = activeEffect.IsInfinite
                ? activeEffect.ElapsedTime
                : Math.Min(activeEffect.ElapsedTime, activeEffect.Duration);

            var safety = 0;
            while (activeEffect.NextPeriodTime <= tickUntil + PFGASHelper.ValueEpsilon)
            {
                var executionResult = ExecuteExecutions(
                    activeEffect.Spec,
                    activeEffect,
                    GameplayEffectExecutionPhase.OnPeriod);
                if (executionResult.Failed)
                {
                    GASGuard.ThrowInvalidOperation(executionResult.Failure);
                }

                var modifiers = modifierResolver.ResolveModifiersOrThrow(
                    activeEffect.Spec,
                    GameplayEffectModifierPhase.Periodic,
                    activeEffect.StackCount);
                ApplyBaseModifiers(modifiers);
                activeEffect.AdvancePeriod();

                safety++;
                if (safety > 10000)
                {
                    GASGuard.ThrowInvalidOperation("GameplayEffect periodic execution exceeded safety limit.");
                }
            }
        }

        private GASResult RemoveActiveEffect(ActiveGameplayEffect activeEffect, bool executeOnRemove)
        {
            if (executeOnRemove)
            {
                var executionResult = ExecuteExecutions(
                    activeEffect.Spec,
                    activeEffect,
                    GameplayEffectExecutionPhase.OnRemove);
                if (executionResult.Failed)
                {
                    return executionResult;
                }
            }

            activeCleanup.Cleanup(
                activeEffect,
                activeEffect.Effect.GrantedTags.Count > 0,
                true);
            activeEffect.MarkExpired();
            return GASResult.Success();
        }

        private GASResult ExecuteExecutions(
            GameplayEffectSpec spec,
            ActiveGameplayEffect activeEffect,
            GameplayEffectExecutionPhase phase)
        {
            for (var i = 0; i < spec.Effect.Executions.Count; i++)
            {
                var executionSpec = spec.Effect.Executions[i];
                if (executionSpec.Phase != phase)
                {
                    continue;
                }

                var result = executionSpec.Execution.Execute(
                    new GameplayEffectExecutionContext(spec, activeEffect, phase));
                if (result.Failed)
                {
                    return result;
                }
            }

            return GASResult.Success();
        }

        private GASResult ActivateTriggers(ActiveGameplayEffect activeEffect)
        {
            for (var i = 0; i < activeEffect.Effect.Triggers.Count; i++)
            {
                var trigger = activeEffect.Effect.Triggers[i];
                var result = trigger.Trigger.Activate(
                    new GameplayEffectTriggerContext(activeEffect.Spec, activeEffect));
                if (result.Failed)
                {
                    return result;
                }

                activeEffect.AddActiveTrigger(trigger);
            }

            return GASResult.Success();
        }

        private AttributeChange[] ApplyBaseModifiers(IReadOnlyList<AttributeModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
            {
                return Array.Empty<AttributeChange>();
            }

            return Owner.Attributes.ApplyBaseModifiers(modifiers);
        }

        private bool IsHandleOwnedByThisContainer(GameplayEffectHandle handle)
        {
            return handle.IsValid && ReferenceEquals(handle.Owner, this);
        }

        private bool IsActiveEffect(ActiveGameplayEffect activeEffect)
        {
            return activeEffect != null && activeEffects.ContainsKey(activeEffect.Handle.Value);
        }

        private static PFTagId[] ToTagArray(IReadOnlyList<PFTagId> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return Array.Empty<PFTagId>();
            }

            var results = new PFTagId[tags.Count];
            for (var i = 0; i < tags.Count; i++)
            {
                results[i] = tags[i];
            }

            return results;
        }

        private static void AppendModifiers(
            IReadOnlyList<AttributeModifier> modifiers,
            List<AttributeModifier> results)
        {
            if (modifiers == null)
            {
                return;
            }

            for (var i = 0; i < modifiers.Count; i++)
            {
                results.Add(modifiers[i]);
            }
        }

        private static void AppendChanges(
            AttributeChange[] changes,
            List<AttributeChange> results)
        {
            if (changes == null)
            {
                return;
            }

            for (var i = 0; i < changes.Length; i++)
            {
                results.Add(changes[i]);
            }
        }

        private readonly struct ApplyPlan
        {
            public ApplyPlan(
                GameplayEffectSpec spec,
                GameplayEffectStackingDecision stackingDecision,
                AttributeModifier[] instantModifiers,
                ModifierSource ongoingSource,
                AttributeModifier[] initialPeriodicModifiers)
            {
                Spec = spec;
                StackingDecision = stackingDecision;
                InstantModifiers = instantModifiers ?? Array.Empty<AttributeModifier>();
                OngoingSource = ongoingSource;
                InitialPeriodicModifiers = initialPeriodicModifiers ?? Array.Empty<AttributeModifier>();
            }

            public GameplayEffectSpec Spec { get; }

            public GameplayEffectStackingDecision StackingDecision { get; }

            public AttributeModifier[] InstantModifiers { get; }

            public ModifierSource OngoingSource { get; }

            public AttributeModifier[] InitialPeriodicModifiers { get; }
        }
    }
}
