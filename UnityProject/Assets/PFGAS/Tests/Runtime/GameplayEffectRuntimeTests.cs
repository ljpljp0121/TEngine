using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFGAS.Runtime.Tests
{
    public sealed class GameplayEffectRuntimeTests : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool stopOnFirstFailure;
        [SerializeField] private int passedCount;
        [SerializeField] private int failedCount;
        [SerializeField] private List<string> lastResults = new List<string>();

        private readonly List<GameObject> objects = new List<GameObject>();

        public int PassedCount => passedCount;

        public int FailedCount => failedCount;

        public IReadOnlyList<string> LastResults => lastResults;

        private void Start()
        {
            if (runOnStart)
            {
                RunAll();
            }
        }

        private void OnDestroy()
        {
            CleanupCreatedObjects();
        }

        [ContextMenu("Run GameplayEffect Runtime Tests")]
        public void RunAll()
        {
            passedCount = 0;
            failedCount = 0;
            lastResults.Clear();

            RunCase("Shared effect creates independent specs and captured values", SharedEffectCreatesIndependentSpecs);
            RunCase("ApplyToSelf creates a spec for instant modifiers", ApplyToSelfCreatesSpecForInstantModifiers);
            RunCase("Target-local ongoing magnitude stays live in AttributeGraph", TargetLocalOngoingMagnitudeStaysLive);
            RunCase("Source dynamic ongoing rebuilds target ModifierSource", SourceDynamicOngoingRebuildsModifierSource);
            RunCase("Source dynamic changes are coalesced until tick", SourceDynamicChangesAreCoalescedUntilTick);
            RunCase("SnapshotOnApply freezes source ongoing magnitude", SnapshotOnApplyFreezesSourceOngoingMagnitude);
            RunCase("ReevaluateOnPeriod reads latest source value", ReevaluateOnPeriodReadsLatestSourceValue);
            RunCase("Tags block application and granted tags expire", TagsBlockApplicationAndGrantedTagsExpire);
            RunCase("Stacking modes update active runtime state", StackingModesUpdateActiveRuntimeState);
            RunCase("Stacking scope and overflow policies are explicit", StackingScopeAndOverflowPoliciesAreExplicit);
            RunCase("Stack snapshot uses first captured value times stack count", StackSnapshotUsesFirstCapturedValueTimesStackCount);
            RunCase("Stack refresh and overflow OnApply semantics are explicit", StackRefreshAndOverflowOnApplySemantics);
            RunCase("Execution and Trigger slots follow active lifecycle", ExecutionAndTriggerSlotsFollowLifecycle);
            RunCase("Trigger activation observes committed active state", TriggerActivationObservesCommittedActiveState);
            RunCase("Source attribute magnitude without source fails atomically", SourceAttributeMagnitudeWithoutSourceFails);
            RunCase("Invalid effect configurations fail before commit", InvalidEffectConfigurationsFailBeforeCommit);
            RunCase("Source and target tag requirements gate apply", SourceAndTargetTagRequirementsGateApply);
            RunCase("Manual remove cleans modifiers tags subscriptions and triggers", ManualRemoveCleansRuntimeResources);
            RunCase("Trigger activation failure rolls back committed resources", TriggerActivationFailureRollsBackCommittedResources);
            RunCase("OnApply execution failure does not commit state", OnApplyExecutionFailureDoesNotCommitState);
            RunCase("OnRemove failure keeps active effect removable", OnRemoveFailureKeepsActiveEffectRemovable);
            RunCase("Replace failure keeps existing active effect", ReplaceFailureKeepsExistingActiveEffect);
            RunCase("Replace trigger failure keeps existing active effect", ReplaceTriggerFailureKeepsExistingActiveEffect);
            RunCase("ReplaceOldest failure keeps oldest active effect", ReplaceOldestFailureKeepsOldestActiveEffect);
            RunCase("Dynamic rebuild failure restores old modifier source", DynamicRebuildFailureRestoresOldModifierSource);
            RunCase("OnApply does not run before prepare failure", OnApplyDoesNotRunBeforePrepareFailure);
            RunCase("GameplayEffect defensively copies constructor inputs", GameplayEffectDefensivelyCopiesConstructorInputs);
            RunCase("RemoveAll cleans multiple active effects", RemoveAllCleansMultipleActiveEffects);

            if (failedCount == 0)
            {
                Debug.Log($"PFGAS GameplayEffect runtime tests passed: {passedCount} cases.", this);
            }
            else
            {
                Debug.LogError(
                    $"PFGAS GameplayEffect runtime tests failed: {failedCount} failed, {passedCount} passed.",
                    this);
            }
        }

        private void RunCase(string caseName, Action action)
        {
            try
            {
                CleanupCreatedObjects();
                action();
                passedCount++;
                lastResults.Add("[PASS] " + caseName);
            }
            catch (Exception exception)
            {
                failedCount++;
                lastResults.Add("[FAIL] " + caseName + ": " + exception.Message);
                Debug.LogException(new InvalidOperationException("[PFGAS] " + caseName, exception), this);
                if (stopOnFirstFailure)
                {
                    throw;
                }
            }
            finally
            {
                CleanupCreatedObjects();
            }
        }

        private void SharedEffectCreatesIndependentSpecs()
        {
            var source = CreateUnit("Source");
            var firstTarget = CreateUnit("FirstTarget");
            var secondTarget = CreateUnit("SecondTarget");
            var effect = new GameplayEffect(
                "SharedSnapshotBuff",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            var firstSpec = firstTarget.Effects.CreateSpec(effect, source, firstTarget);
            Expect(firstSpec.Succeeded, "First spec should be created.");

            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
            var secondSpec = secondTarget.Effects.CreateSpec(effect, source, secondTarget);
            Expect(secondSpec.Succeeded, "Second spec should be created.");

            var firstApply = firstTarget.Effects.Apply(firstSpec.Value);
            var secondApply = secondTarget.Effects.Apply(secondSpec.Value);

            Expect(firstApply.Succeeded, "First spec apply should succeed.");
            Expect(secondApply.Succeeded, "Second spec apply should succeed.");
            ExpectNearly(firstTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "First spec captured source value.");
            ExpectNearly(secondTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 200f, "Second spec captured source value.");
            ExpectEqual(firstSpec.Value.Target, firstTarget, "First spec target.");
            ExpectEqual(secondSpec.Value.Target, secondTarget, "Second spec target.");
        }

        private void ApplyToSelfCreatesSpecForInstantModifiers()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "HeavyStrike",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, -0.3f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.TargetAttribute(PFAttributeId.MaxHP, 0.1f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Succeeded, "Instant apply should succeed.");
            Expect(!result.Value.Handle.IsValid, "Instant apply should not return an active handle.");
            ExpectNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 80f, "Target HP base after instant damage.");
            ExpectEqual(result.Value.AttributeChanges.Count, 1, "Instant apply should report one changed attribute.");
            ExpectEqual(result.Value.AttributeChanges[0].AttributeId, PFAttributeId.HP, "Instant change attribute.");
            ExpectNearly(result.Value.AttributeChanges[0].OldBaseValue, 100f, "Instant old base value.");
            ExpectNearly(result.Value.AttributeChanges[0].NewBaseValue, 80f, "Instant new base value.");
        }

        private void TargetLocalOngoingMagnitudeStaysLive()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            target.Attributes.SetBaseValue(PFAttributeId.HP, 0f);

            var effect = new GameplayEffect(
                "TargetLocalShield",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.TargetAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Succeeded, "Target-local ongoing apply should succeed.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 50f, "Initial target-local ongoing value.");

            target.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);

            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 100f, "Target-local ongoing value should track MaxHP.");
            target.Effects.Tick(5f);
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 0f, "Target-local ongoing value should be removed.");
        }

        private void SourceDynamicOngoingRebuildsModifierSource()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "DynamicLeadershipBuff",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Succeeded, "Source dynamic ongoing apply should succeed.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Initial source dynamic value.");

            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);

            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Source dynamic value should wait for tick boundary.");
            target.Effects.Tick(0f);

            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 200f, "Source dynamic value should rebuild ModifierSource.");
            target.Effects.Tick(5f);
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Source dynamic effect should expire.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Source dynamic modifier should be removed.");
        }

        private void SourceDynamicChangesAreCoalescedUntilTick()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var targetChangeEvents = 0;
            var effect = new GameplayEffect(
                "CoalescedDynamicLeadershipBuff",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            var result = target.Effects.ApplyToSelf(effect, source);
            Expect(result.Succeeded, "Coalesced dynamic apply should succeed.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Initial coalesced dynamic value.");

            target.Attributes.AttributeChanged += change =>
            {
                if (change.AttributeId == PFAttributeId.MaxHP)
                {
                    targetChangeEvents++;
                }
            };

            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 120f);
            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 140f);
            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);

            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Target should keep old value before dirty flush.");
            ExpectEqual(targetChangeEvents, 0, "Source changes should not immediately publish target changes.");

            target.Effects.Tick(0f);

            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 200f, "Dirty flush should use latest source value.");
            ExpectEqual(targetChangeEvents, 1, "Dirty flush should publish one coalesced target change.");
        }

        private void SnapshotOnApplyFreezesSourceOngoingMagnitude()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "SnapshotLeadershipBuff",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Succeeded, "Snapshot ongoing apply should succeed.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Initial snapshot value.");

            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);

            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Snapshot value should not change.");
            target.Effects.Tick(5f);
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Snapshot modifier should be removed.");
        }

        private void ReevaluateOnPeriodReadsLatestSourceValue()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "Burning",
                GameplayEffectLifetime.ForDuration(3f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, -0.1f),
                        GameplayEffectCapturePolicy.ReevaluateOnPeriod),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Succeeded, "Periodic apply should succeed.");
            target.Effects.Tick(1f);
            ExpectNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 90f, "First periodic tick.");

            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
            target.Effects.Tick(1f);
            ExpectNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 70f, "Second periodic tick should use updated source.");

            target.Effects.Tick(1f);
            ExpectNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 50f, "Third periodic tick at expiry.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Periodic effect should expire.");
        }

        private void TagsBlockApplicationAndGrantedTagsExpire()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            target.Tags.AddLooseTag(PFTagId.State_DeBuff_Fire);

            var blocked = new GameplayEffect(
                "BlockedByDebuff",
                GameplayEffectLifetime.ForDuration(1f),
                tags: new GameplayEffectTagRequirements(
                    blockedTargetTags: new[] { PFTagId.State_DeBuff }));

            var blockedResult = target.Effects.ApplyToSelf(blocked, source);

            Expect(blockedResult.Failed, "Blocked target tag should fail apply.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Blocked effect should not become active.");

            target.Tags.RemoveLooseTag(PFTagId.State_DeBuff_Fire);
            var grant = new GameplayEffect(
                "GrantBurning",
                GameplayEffectLifetime.ForDuration(1f),
                grantedTags: new[] { PFTagId.State_DeBuff_Fire });

            var grantResult = target.Effects.ApplyToSelf(grant, source);

            Expect(grantResult.Succeeded, "Granted tag effect should apply.");
            Expect(target.Tags.HasTag(PFTagId.State_DeBuff_Fire), "Granted tag should be present.");

            target.Effects.Tick(1f);

            Expect(!target.Tags.HasTag(PFTagId.State_DeBuff_Fire), "Granted tag should be removed on expire.");
        }

        private void StackingModesUpdateActiveRuntimeState()
        {
            var target = CreateUnit("IndependentTarget");
            var independent = StackBuff("Independent", GameplayEffectStackingPolicy.Independent());

            var independentFirst = target.Effects.ApplyToSelf(independent, target);
            var independentSecond = target.Effects.ApplyToSelf(independent, target);

            Expect(independentFirst.Succeeded, "First independent apply should succeed.");
            Expect(independentSecond.Succeeded, "Second independent apply should succeed.");
            ExpectEqual(target.Effects.ActiveEffectCount, 2, "Independent stacking should create separate active effects.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 120f, "Independent modifiers should both contribute.");

            var refreshTarget = CreateUnit("RefreshTarget");
            var refresh = StackBuff("Refresh", GameplayEffectStackingPolicy.Refresh());
            var refreshFirst = refreshTarget.Effects.ApplyToSelf(refresh, refreshTarget);
            refreshTarget.Effects.Tick(3f);
            var refreshSecond = refreshTarget.Effects.ApplyToSelf(refresh, refreshTarget);

            Expect(refreshFirst.Succeeded, "First refresh apply should succeed.");
            Expect(refreshSecond.Succeeded, "Second refresh apply should succeed.");
            ExpectEqual(refreshTarget.Effects.ActiveEffectCount, 1, "Refresh stacking should keep one active effect.");
            Expect(refreshTarget.Effects.TryGetActiveEffect(refreshFirst.Value.Handle, out var refreshed), "Refresh handle should remain active.");
            ExpectEqual(refreshed.StackCount, 1, "Refresh stacking should not increase stack count.");
            ExpectNearly(refreshed.RemainingTime, 5f, "Refresh stacking should reset remaining duration.");

            var replaceTarget = CreateUnit("ReplaceTarget");
            var replace = StackBuff("Replace", GameplayEffectStackingPolicy.Replace());
            var replaceFirst = replaceTarget.Effects.ApplyToSelf(replace, replaceTarget);
            var replaceSecond = replaceTarget.Effects.ApplyToSelf(replace, replaceTarget);

            Expect(replaceFirst.Succeeded, "First replace apply should succeed.");
            Expect(replaceSecond.Succeeded, "Second replace apply should succeed.");
            ExpectEqual(replaceTarget.Effects.ActiveEffectCount, 1, "Replace stacking should keep one active effect.");
            Expect(!replaceTarget.Effects.TryGetActiveEffect(replaceFirst.Value.Handle, out _), "Replace should remove old active effect.");
            Expect(replaceTarget.Effects.TryGetActiveEffect(replaceSecond.Value.Handle, out _), "Replace should create new active effect.");
            ExpectNearly(replaceTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f, "Replace modifier should not duplicate.");

            var stackTarget = CreateUnit("StackTarget");
            var stack = StackBuff("Stack", GameplayEffectStackingPolicy.Stack(3), scaleByStackCount: true);
            var stackFirst = stackTarget.Effects.ApplyToSelf(stack, stackTarget);
            var stackSecond = stackTarget.Effects.ApplyToSelf(stack, stackTarget);
            var stackThird = stackTarget.Effects.ApplyToSelf(stack, stackTarget);

            Expect(stackFirst.Succeeded, "First stack apply should succeed.");
            Expect(stackSecond.Succeeded, "Second stack apply should succeed.");
            Expect(stackThird.Succeeded, "Third stack apply should succeed.");
            ExpectEqual(stackTarget.Effects.ActiveEffectCount, 1, "Stack mode should keep one active effect.");
            Expect(stackTarget.Effects.TryGetActiveEffect(stackFirst.Value.Handle, out var stacked), "Stack handle should remain active.");
            ExpectEqual(stacked.StackCount, 3, "Stack mode should increase stack count.");
            ExpectNearly(stackTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 130f, "Stacked ongoing modifier should scale by stack count.");
        }

        private void StackingScopeAndOverflowPoliciesAreExplicit()
        {
            var sourceA = CreateUnit("SourceA");
            var sourceB = CreateUnit("SourceB");
            var scopedTarget = CreateUnit("ScopedTarget");
            var scoped = StackBuff(
                "Scoped",
                GameplayEffectStackingPolicy.Stack(
                    2,
                    GameplayEffectStackingScope.BySourceAndTarget),
                scaleByStackCount: true);

            var scopedA = scopedTarget.Effects.ApplyToSelf(scoped, sourceA);
            var scopedB = scopedTarget.Effects.ApplyToSelf(scoped, sourceB);

            Expect(scopedA.Succeeded, "Scoped source A apply should succeed.");
            Expect(scopedB.Succeeded, "Scoped source B apply should succeed.");
            ExpectEqual(scopedTarget.Effects.ActiveEffectCount, 2, "BySourceAndTarget should keep source stacks separate.");
            ExpectNearly(scopedTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 120f, "Scoped source modifiers should both contribute.");

            var failTarget = CreateUnit("OverflowFailTarget");
            var fail = StackBuff(
                "OverflowFail",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.Fail),
                scaleByStackCount: true);
            var failFirst = failTarget.Effects.ApplyToSelf(fail, failTarget);
            var failSecond = failTarget.Effects.ApplyToSelf(fail, failTarget);

            Expect(failFirst.Succeeded, "First overflow fail apply should succeed.");
            Expect(failSecond.Failed, "Overflow fail should reject apply.");
            ExpectEqual(failTarget.Effects.ActiveEffectCount, 1, "Overflow fail should leave active count unchanged.");
            ExpectNearly(failTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f, "Overflow fail should leave attributes unchanged.");

            var ignoreTarget = CreateUnit("OverflowIgnoreTarget");
            var ignore = StackBuff(
                "OverflowIgnore",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.Ignore),
                scaleByStackCount: true);
            var ignoreFirst = ignoreTarget.Effects.ApplyToSelf(ignore, ignoreTarget);
            var ignoreSecond = ignoreTarget.Effects.ApplyToSelf(ignore, ignoreTarget);

            Expect(ignoreFirst.Succeeded, "First overflow ignore apply should succeed.");
            Expect(ignoreSecond.Succeeded, "Overflow ignore should return success.");
            Expect(ignoreTarget.Effects.TryGetActiveEffect(ignoreFirst.Value.Handle, out var ignored), "Ignore handle should remain active.");
            ExpectEqual(ignored.StackCount, 1, "Overflow ignore should not increase stack count.");

            var refreshTarget = CreateUnit("OverflowRefreshTarget");
            var refresh = StackBuff(
                "OverflowRefresh",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.Refresh),
                scaleByStackCount: true);
            var refreshFirst = refreshTarget.Effects.ApplyToSelf(refresh, refreshTarget);
            refreshTarget.Effects.Tick(3f);
            var refreshSecond = refreshTarget.Effects.ApplyToSelf(refresh, refreshTarget);

            Expect(refreshFirst.Succeeded, "First overflow refresh apply should succeed.");
            Expect(refreshSecond.Succeeded, "Overflow refresh should return success.");
            Expect(refreshTarget.Effects.TryGetActiveEffect(refreshFirst.Value.Handle, out var refreshed), "Refresh overflow handle should remain active.");
            ExpectEqual(refreshed.StackCount, 1, "Overflow refresh should not increase stack count.");
            ExpectNearly(refreshed.RemainingTime, 5f, "Overflow refresh should reset duration.");

            var replaceTarget = CreateUnit("OverflowReplaceTarget");
            var replace = StackBuff(
                "OverflowReplace",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.ReplaceOldest),
                scaleByStackCount: true);
            var replaceFirst = replaceTarget.Effects.ApplyToSelf(replace, replaceTarget);
            var replaceSecond = replaceTarget.Effects.ApplyToSelf(replace, replaceTarget);

            Expect(replaceFirst.Succeeded, "First overflow replace apply should succeed.");
            Expect(replaceSecond.Succeeded, "Overflow replace should apply a new active effect.");
            ExpectEqual(replaceTarget.Effects.ActiveEffectCount, 1, "Overflow replace should keep one active effect.");
            Expect(!replaceTarget.Effects.TryGetActiveEffect(replaceFirst.Value.Handle, out _), "Overflow replace should remove oldest effect.");
            Expect(replaceTarget.Effects.TryGetActiveEffect(replaceSecond.Value.Handle, out _), "Overflow replace should keep new effect.");
        }

        private void StackSnapshotUsesFirstCapturedValueTimesStackCount()
        {
            var source = CreateUnit("StackSnapshotSource");
            var target = CreateUnit("StackSnapshotTarget");
            var effect = new GameplayEffect(
                "RefreshCapturedStack",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.SnapshotOnApply,
                        scaleByStackCount: true),
                },
                stacking: GameplayEffectStackingPolicy.Stack(2));

            var first = target.Effects.ApplyToSelf(effect, source);
            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
            var second = target.Effects.ApplyToSelf(effect, source);

            Expect(first.Succeeded, "First snapshot stack apply should succeed.");
            Expect(second.Succeeded, "Second snapshot stack apply should succeed.");
            Expect(target.Effects.TryGetActiveEffect(first.Value.Handle, out var active), "Snapshot stack handle should remain active.");
            ExpectEqual(active.StackCount, 2, "Snapshot stack should increase stack count.");
            ExpectNearly(
                target.Attributes.GetCurrentValue(PFAttributeId.MaxHP),
                200f,
                "Stack snapshot strategy should keep first captured value and multiply by stack count.");
        }

        private void StackRefreshAndOverflowOnApplySemantics()
        {
            var stackCounters = new LifecycleCounters();
            var stackTarget = CreateUnit("StackOnApplyTarget");
            var stack = StackBuffWithOnApply(
                "StackRunsOnApply",
                GameplayEffectStackingPolicy.Stack(2),
                stackCounters,
                scaleByStackCount: true);

            var stackFirst = stackTarget.Effects.ApplyToSelf(stack, stackTarget);
            var stackSecond = stackTarget.Effects.ApplyToSelf(stack, stackTarget);

            Expect(stackFirst.Succeeded, "First stack apply should succeed.");
            Expect(stackSecond.Succeeded, "Second stack apply should succeed.");
            ExpectEqual(stackCounters.ApplyCount, 2, "Successful stack reapply should run OnApply.");

            var refreshCounters = new LifecycleCounters();
            var refreshTarget = CreateUnit("RefreshOnApplyTarget");
            var refresh = StackBuffWithOnApply(
                "RefreshSkipsOnApply",
                GameplayEffectStackingPolicy.Refresh(),
                refreshCounters);

            var refreshFirst = refreshTarget.Effects.ApplyToSelf(refresh, refreshTarget);
            var refreshSecond = refreshTarget.Effects.ApplyToSelf(refresh, refreshTarget);

            Expect(refreshFirst.Succeeded, "First refresh apply should succeed.");
            Expect(refreshSecond.Succeeded, "Refresh reapply should succeed.");
            ExpectEqual(refreshCounters.ApplyCount, 1, "Refresh-only reapply should not run OnApply.");

            var ignoreCounters = new LifecycleCounters();
            var ignoreTarget = CreateUnit("IgnoreOverflowOnApplyTarget");
            var ignore = StackBuffWithOnApply(
                "IgnoreOverflowSkipsOnApply",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.Ignore),
                ignoreCounters,
                scaleByStackCount: true);

            var ignoreFirst = ignoreTarget.Effects.ApplyToSelf(ignore, ignoreTarget);
            var ignoreSecond = ignoreTarget.Effects.ApplyToSelf(ignore, ignoreTarget);

            Expect(ignoreFirst.Succeeded, "First ignore overflow apply should succeed.");
            Expect(ignoreSecond.Succeeded, "Ignore overflow apply should succeed.");
            ExpectEqual(ignoreCounters.ApplyCount, 1, "Ignore overflow should not run OnApply.");

            var overflowRefreshCounters = new LifecycleCounters();
            var overflowRefreshTarget = CreateUnit("RefreshOverflowOnApplyTarget");
            var overflowRefresh = StackBuffWithOnApply(
                "RefreshOverflowSkipsOnApply",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.Refresh),
                overflowRefreshCounters,
                scaleByStackCount: true);

            var overflowRefreshFirst = overflowRefreshTarget.Effects.ApplyToSelf(overflowRefresh, overflowRefreshTarget);
            var overflowRefreshSecond = overflowRefreshTarget.Effects.ApplyToSelf(overflowRefresh, overflowRefreshTarget);

            Expect(overflowRefreshFirst.Succeeded, "First refresh overflow apply should succeed.");
            Expect(overflowRefreshSecond.Succeeded, "Refresh overflow apply should succeed.");
            ExpectEqual(overflowRefreshCounters.ApplyCount, 1, "Refresh overflow should not run OnApply.");

            var replaceCounters = new LifecycleCounters();
            var replaceTarget = CreateUnit("ReplaceOldestOnApplyTarget");
            var replace = StackBuffWithOnApply(
                "ReplaceOldestRunsOnApply",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.ReplaceOldest),
                replaceCounters,
                scaleByStackCount: true);

            var replaceFirst = replaceTarget.Effects.ApplyToSelf(replace, replaceTarget);
            var replaceSecond = replaceTarget.Effects.ApplyToSelf(replace, replaceTarget);

            Expect(replaceFirst.Succeeded, "First ReplaceOldest apply should succeed.");
            Expect(replaceSecond.Succeeded, "ReplaceOldest apply should succeed.");
            ExpectEqual(replaceCounters.ApplyCount, 2, "ReplaceOldest should follow replace semantics and run OnApply for the new apply.");
        }

        private void ExecutionAndTriggerSlotsFollowLifecycle()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "LifecycleEffect",
                GameplayEffectLifetime.ForDuration(1f, period: 0.5f),
                executions: new[]
                {
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnApply,
                        new CounterExecution(counters, GameplayEffectExecutionPhase.OnApply)),
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnPeriod,
                        new CounterExecution(counters, GameplayEffectExecutionPhase.OnPeriod)),
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnRemove,
                        new CounterExecution(counters, GameplayEffectExecutionPhase.OnRemove)),
                },
                triggers: new[]
                {
                    new GameplayEffectTriggerSpec(new EventCounterTrigger("Hit", counters)),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Succeeded, "Lifecycle effect should apply.");
            ExpectEqual(counters.ApplyCount, 1, "OnApply execution should run.");

            target.GameplayEventBus.Publish("Hit", source, target);
            ExpectEqual(counters.TriggerCount, 1, "Trigger should respond while active.");

            target.Effects.Tick(0.5f);
            ExpectEqual(counters.PeriodCount, 1, "OnPeriod execution should run on period boundary.");

            target.Effects.Tick(0.5f);
            ExpectEqual(counters.RemoveCount, 1, "OnRemove execution should run on expiry.");
            ExpectEqual(counters.DeactivateCount, 1, "Trigger should deactivate on removal.");

            target.GameplayEventBus.Publish("Hit", source, target);
            ExpectEqual(counters.TriggerCount, 1, "Removed trigger should not respond.");
        }

        private void TriggerActivationObservesCommittedActiveState()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "ObservableTriggerActivation",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                grantedTags: new[] { PFTagId.State_Buff },
                triggers: new[]
                {
                    new GameplayEffectTriggerSpec(
                        new ObservingActivationTrigger(
                            counters,
                            PFTagId.State_Buff,
                            expectedMaxHp: 110f)),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Succeeded, "Observable trigger effect should apply.");
            ExpectEqual(counters.TriggerObservedActiveState, 1, "Trigger should observe committed active state during activation.");
        }

        private void SourceAttributeMagnitudeWithoutSourceFails()
        {
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "MissingSourceDamage",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, -1f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            var result = target.Effects.ApplyToSelf(effect);

            Expect(result.Failed, "Missing source should fail apply.");
            ExpectNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 100f, "Failed apply should not change HP.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Failed apply should not become active.");
        }

        private void InvalidEffectConfigurationsFailBeforeCommit()
        {
            var target = CreateUnit("Target");

            var instantWithOngoing = new GameplayEffect(
                "InstantWithOngoing",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            ExpectThrows<InvalidOperationException>(
                () => target.Effects.ApplyToSelf(instantWithOngoing, target),
                "Instant effect with ongoing modifier");

            var periodicWithoutPeriod = new GameplayEffect(
                "PeriodicWithoutPeriod",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-5f),
                        GameplayEffectCapturePolicy.ReevaluateOnPeriod),
                });

            ExpectThrows<InvalidOperationException>(
                () => target.Effects.ApplyToSelf(periodicWithoutPeriod, target),
                "Periodic effect without positive period");

            var instantWithGrantedTag = new GameplayEffect(
                "InstantWithGrantedTag",
                GameplayEffectLifetime.Instant,
                grantedTags: new[] { PFTagId.State_Buff });

            ExpectThrows<InvalidOperationException>(
                () => target.Effects.ApplyToSelf(instantWithGrantedTag, target),
                "Instant effect with granted tag");

            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Invalid configurations should not create active effects.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Invalid configurations should not add modifiers.");
            Expect(!target.Tags.HasTag(PFTagId.State_Buff), "Invalid configurations should not grant tags.");
        }

        private void SourceAndTargetTagRequirementsGateApply()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var gated = new GameplayEffect(
                "TagGatedBuff",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                new GameplayEffectTagRequirements(
                    requiredSourceTags: new[] { PFTagId.State_Buff },
                    blockedSourceTags: new[] { PFTagId.State_DeBuff },
                    requiredTargetTags: new[] { PFTagId.Life },
                    blockedTargetTags: new[] { PFTagId.State_DeBuff }));

            var missingSourceTag = target.Effects.ApplyToSelf(gated, source);
            Expect(missingSourceTag.Failed, "Missing source required tag should fail.");

            source.Tags.AddLooseTag(PFTagId.State_Buff);
            var missingTargetTag = target.Effects.ApplyToSelf(gated, source);
            Expect(missingTargetTag.Failed, "Missing target required tag should fail.");

            target.Tags.AddLooseTag(PFTagId.Life_HP);
            source.Tags.AddLooseTag(PFTagId.State_DeBuff_Ice);
            var blockedSource = target.Effects.ApplyToSelf(gated, source);
            Expect(blockedSource.Failed, "Blocked source tag should fail.");

            source.Tags.RemoveLooseTag(PFTagId.State_DeBuff_Ice);
            target.Tags.AddLooseTag(PFTagId.State_DeBuff_Fire);
            var blockedTarget = target.Effects.ApplyToSelf(gated, source);
            Expect(blockedTarget.Failed, "Blocked target tag should fail.");

            target.Tags.RemoveLooseTag(PFTagId.State_DeBuff_Fire);
            var success = target.Effects.ApplyToSelf(gated, source);

            Expect(success.Succeeded, "Satisfied tag requirements should allow apply.");
            ExpectEqual(target.Effects.ActiveEffectCount, 1, "Successful tag-gated effect should become active.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f, "Successful tag-gated modifier.");
        }

        private void ManualRemoveCleansRuntimeResources()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "ManualRemoveCleanup",
                GameplayEffectLifetime.ForDuration(100f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                },
                grantedTags: new[] { PFTagId.State_Buff },
                executions: new[]
                {
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnRemove,
                        new CounterExecution(counters, GameplayEffectExecutionPhase.OnRemove)),
                },
                triggers: new[]
                {
                    new GameplayEffectTriggerSpec(new EventCounterTrigger("Hit", counters)),
                });

            var apply = target.Effects.ApplyToSelf(effect, source);

            Expect(apply.Succeeded, "Cleanup effect should apply.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Cleanup effect initial modifier.");
            Expect(target.Tags.HasTag(PFTagId.State_Buff), "Cleanup effect should grant tag.");
            target.GameplayEventBus.Publish("Hit", source, target);
            ExpectEqual(counters.TriggerCount, 1, "Cleanup trigger should respond before remove.");

            var remove = target.Effects.Remove(apply.Value.Handle);

            Expect(remove.Succeeded, "Manual remove should succeed.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Manual remove should clear active effect.");
            ExpectEqual(counters.RemoveCount, 1, "Manual remove should run OnRemove.");
            ExpectEqual(counters.DeactivateCount, 1, "Manual remove should deactivate trigger.");
            Expect(!target.Tags.HasTag(PFTagId.State_Buff), "Manual remove should remove granted tag.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Manual remove should remove modifier.");

            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
            target.GameplayEventBus.Publish("Hit", source, target);

            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Removed dynamic subscription should not rebuild.");
            ExpectEqual(counters.TriggerCount, 1, "Removed trigger should not respond.");
        }

        private void TriggerActivationFailureRollsBackCommittedResources()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "FailingTriggerEffect",
                GameplayEffectLifetime.ForDuration(10f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                grantedTags: new[] { PFTagId.State_Buff },
                triggers: new[]
                {
                    new GameplayEffectTriggerSpec(new EventCounterTrigger("Hit", counters)),
                    new GameplayEffectTriggerSpec(new FailingTrigger(counters)),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Failed, "Failing trigger should fail apply.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Failing trigger should not create active effect.");
            ExpectEqual(counters.TriggerActivationFailures, 1, "Failing trigger activation count.");
            ExpectEqual(counters.DeactivateCount, 1, "Previously activated trigger should be deactivated.");
            Expect(!target.Tags.HasTag(PFTagId.State_Buff), "Failing trigger should rollback granted tag.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Failing trigger should rollback modifier.");

            target.GameplayEventBus.Publish("Hit", source, target);
            ExpectEqual(counters.TriggerCount, 0, "Rolled-back trigger should not respond.");
        }

        private void OnApplyExecutionFailureDoesNotCommitState()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "FailingOnApply",
                GameplayEffectLifetime.ForDuration(10f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-25f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                grantedTags: new[] { PFTagId.State_Buff },
                executions: new[]
                {
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnApply,
                        new FailingExecution(counters)),
                });

            var result = target.Effects.ApplyToSelf(effect, source);

            Expect(result.Failed, "Failing OnApply execution should fail apply.");
            ExpectEqual(counters.ApplyFailureCount, 1, "Failing OnApply execution count.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Failing OnApply should not create active effect.");
            ExpectNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 100f, "Failing OnApply should not apply instant modifier.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Failing OnApply should not add ongoing modifier.");
            Expect(!target.Tags.HasTag(PFTagId.State_Buff), "Failing OnApply should not grant tag.");
        }

        private void OnRemoveFailureKeepsActiveEffectRemovable()
        {
            var source = CreateUnit("RemoveFailureSource");
            var target = CreateUnit("RemoveFailureTarget");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "FailingOnRemoveOnce",
                GameplayEffectLifetime.ForDuration(10f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                grantedTags: new[] { PFTagId.State_Buff },
                executions: new[]
                {
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnRemove,
                        new FailingFirstRemoveExecution(counters)),
                });

            var apply = target.Effects.ApplyToSelf(effect, source);
            var firstRemove = target.Effects.Remove(apply.Value.Handle);

            Expect(apply.Succeeded, "OnRemove failure setup should apply.");
            Expect(firstRemove.Failed, "First remove should fail from OnRemove execution.");
            ExpectEqual(counters.RemoveFailureCount, 1, "First OnRemove failure should be recorded.");
            ExpectEqual(target.Effects.ActiveEffectCount, 1, "Failed OnRemove should keep active effect.");
            Expect(target.Effects.TryGetActiveEffect(apply.Value.Handle, out _), "Failed OnRemove should keep handle removable.");
            Expect(target.Tags.HasTag(PFTagId.State_Buff), "Failed OnRemove should keep granted tag.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f, "Failed OnRemove should keep modifier.");

            var secondRemove = target.Effects.Remove(apply.Value.Handle);

            Expect(secondRemove.Succeeded, "Second remove should succeed.");
            ExpectEqual(counters.RemoveCount, 1, "Second OnRemove should complete.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Successful retry should remove active effect.");
            Expect(!target.Tags.HasTag(PFTagId.State_Buff), "Successful retry should remove granted tag.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Successful retry should remove modifier.");
        }

        private void ReplaceFailureKeepsExistingActiveEffect()
        {
            var source = CreateUnit("ReplaceFailureSource");
            var target = CreateUnit("ReplaceFailureTarget");
            var effect = SourceDynamicStackBuff(
                "ReplaceFailure",
                GameplayEffectStackingPolicy.Replace());

            var first = target.Effects.ApplyToSelf(effect, source);
            var secondSpec = target.Effects.CreateSpec(effect, source, target);
            source.Attributes.RemoveAttribute(PFAttributeId.HP);
            var second = target.Effects.Apply(secondSpec.Value);

            Expect(first.Succeeded, "Initial replace effect should apply.");
            Expect(secondSpec.Succeeded, "Second replace spec should be created before source changes.");
            Expect(second.Failed, "Second replace apply should fail after source attribute is removed.");
            ExpectEqual(target.Effects.ActiveEffectCount, 1, "Failed replace should keep old active count.");
            Expect(target.Effects.TryGetActiveEffect(first.Value.Handle, out _), "Failed replace should keep old handle active.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Failed replace should keep old modifier.");
        }

        private void ReplaceTriggerFailureKeepsExistingActiveEffect()
        {
            var source = CreateUnit("ReplaceTriggerFailureSource");
            var target = CreateUnit("ReplaceTriggerFailureTarget");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "ReplaceTriggerFailure",
                GameplayEffectLifetime.ForDuration(10f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                stacking: GameplayEffectStackingPolicy.Replace(),
                triggers: new[]
                {
                    new GameplayEffectTriggerSpec(new FailingSecondActivationTrigger(counters)),
                });

            var first = target.Effects.ApplyToSelf(effect, source);
            var second = target.Effects.ApplyToSelf(effect, source);

            Expect(first.Succeeded, "Initial replace trigger effect should apply.");
            Expect(second.Failed, "Second replace apply should fail during trigger activation.");
            ExpectEqual(counters.TriggerActivationFailures, 1, "Second trigger activation should fail once.");
            ExpectEqual(target.Effects.ActiveEffectCount, 1, "Failed replace trigger should keep old active count.");
            Expect(target.Effects.TryGetActiveEffect(first.Value.Handle, out _), "Failed replace trigger should keep old handle active.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f, "Failed replace trigger should keep old modifier.");
        }

        private void ReplaceOldestFailureKeepsOldestActiveEffect()
        {
            var source = CreateUnit("ReplaceOldestFailureSource");
            var target = CreateUnit("ReplaceOldestFailureTarget");
            var effect = SourceDynamicStackBuff(
                "ReplaceOldestFailure",
                GameplayEffectStackingPolicy.Stack(
                    1,
                    overflowPolicy: GameplayEffectOverflowPolicy.ReplaceOldest));

            var first = target.Effects.ApplyToSelf(effect, source);
            var secondSpec = target.Effects.CreateSpec(effect, source, target);
            source.Attributes.RemoveAttribute(PFAttributeId.HP);
            var second = target.Effects.Apply(secondSpec.Value);

            Expect(first.Succeeded, "Initial replace-oldest effect should apply.");
            Expect(secondSpec.Succeeded, "Second replace-oldest spec should be created before source changes.");
            Expect(second.Failed, "ReplaceOldest apply should fail after source attribute is removed.");
            ExpectEqual(target.Effects.ActiveEffectCount, 1, "Failed ReplaceOldest should keep old active count.");
            Expect(target.Effects.TryGetActiveEffect(first.Value.Handle, out _), "Failed ReplaceOldest should keep oldest handle active.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Failed ReplaceOldest should keep oldest modifier.");
        }

        private void DynamicRebuildFailureRestoresOldModifierSource()
        {
            var source = CreateUnit("DynamicRebuildFailureSource");
            var target = CreateUnit("DynamicRebuildFailureTarget");
            var effect = new GameplayEffect(
                "DynamicRebuildFailure",
                GameplayEffectLifetime.ForDuration(10f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.HP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            var apply = target.Effects.ApplyToSelf(effect, source);
            source.Attributes.SetBaseValue(PFAttributeId.HP, 80f);
            source.Attributes.RemoveAttribute(PFAttributeId.HP);

            Expect(apply.Succeeded, "Dynamic rebuild failure setup should apply.");
            ExpectThrows<InvalidOperationException>(
                () => target.Effects.Tick(0f),
                "Dynamic rebuild with missing source attribute");
            ExpectEqual(target.Effects.ActiveEffectCount, 1, "Failed dynamic rebuild should keep active effect.");
            Expect(target.Effects.TryGetActiveEffect(apply.Value.Handle, out _), "Failed dynamic rebuild should keep handle active.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 150f, "Failed dynamic rebuild should restore old modifier source.");

            var remove = target.Effects.Remove(apply.Value.Handle);

            Expect(remove.Succeeded, "Effect should remain removable after failed dynamic rebuild.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Remove after failed dynamic rebuild should clean old modifier source.");
        }

        private void OnApplyDoesNotRunBeforePrepareFailure()
        {
            var target = CreateUnit("PrepareFailureTarget");
            var counters = new LifecycleCounters();
            var effect = new GameplayEffect(
                "PrepareFailureSkipsOnApply",
                GameplayEffectLifetime.ForDuration(10f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.HP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                },
                executions: new[]
                {
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnApply,
                        new CounterExecution(counters, GameplayEffectExecutionPhase.OnApply)),
                });

            var result = target.Effects.ApplyToSelf(effect, null);

            Expect(result.Failed, "Missing source should fail during prepare.");
            ExpectEqual(counters.ApplyCount, 0, "OnApply should not run when prepare fails.");
            ExpectEqual(target.Effects.ActiveEffectCount, 0, "Prepare failure should not create active effect.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "Prepare failure should not add modifiers.");
        }

        private void GameplayEffectDefensivelyCopiesConstructorInputs()
        {
            var source = CreateUnit("CopySource");
            var target = CreateUnit("CopyTarget");
            var counters = new LifecycleCounters();
            target.Tags.AddSourceTags("RequiredTag", PFTagId.State_Buff);

            var requiredTags = new[] { PFTagId.State_Buff };
            var grantedTags = new[] { PFTagId.State_DeBuff_Ice };
            var modifiers = new[]
            {
                new GameplayEffectModifierSpec(
                    GameplayEffectModifierPhase.Ongoing,
                    PFAttributeId.MaxHP,
                    GEOperation.Add,
                    GameplayEffectMagnitudeSpec.Fixed(10f),
                    GameplayEffectCapturePolicy.SnapshotOnApply),
            };
            var executions = new[]
            {
                new GameplayEffectExecutionSpec(
                    GameplayEffectExecutionPhase.OnApply,
                    new CounterExecution(counters, GameplayEffectExecutionPhase.OnApply)),
            };
            var triggers = new[]
            {
                new GameplayEffectTriggerSpec(new EventCounterTrigger("CopyEvent", counters)),
            };

            var effect = new GameplayEffect(
                "DefensiveCopy",
                GameplayEffectLifetime.ForDuration(10f),
                modifiers,
                new GameplayEffectTagRequirements(requiredTargetTags: requiredTags),
                grantedTags: grantedTags,
                executions: executions,
                triggers: triggers);

            requiredTags[0] = PFTagId.State_DeBuff_Fire;
            grantedTags[0] = PFTagId.State_DeBuff_Fire;
            modifiers[0] = new GameplayEffectModifierSpec(
                GameplayEffectModifierPhase.Instant,
                PFAttributeId.HP,
                GEOperation.Add,
                GameplayEffectMagnitudeSpec.Fixed(-50f),
                GameplayEffectCapturePolicy.SnapshotOnApply);
            executions[0] = new GameplayEffectExecutionSpec(
                GameplayEffectExecutionPhase.OnApply,
                new FailingExecution(counters));
            triggers[0] = new GameplayEffectTriggerSpec(new FailingTrigger(counters));

            var result = target.Effects.ApplyToSelf(effect, source);
            target.GameplayEventBus.Publish("CopyEvent", source, target);

            Expect(result.Succeeded, "Defensively copied effect should ignore caller array mutations.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f, "Defensive copy should keep original modifier.");
            ExpectNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 100f, "Defensive copy should ignore mutated instant modifier.");
            ExpectEqual(counters.ApplyCount, 1, "Defensive copy should keep original execution.");
            ExpectEqual(counters.ApplyFailureCount, 0, "Defensive copy should ignore mutated failing execution.");
            ExpectEqual(counters.TriggerActivationFailures, 0, "Defensive copy should ignore mutated failing trigger.");
            ExpectEqual(counters.TriggerCount, 1, "Defensive copy should keep original trigger.");
            Expect(target.Tags.HasTag(PFTagId.State_DeBuff_Ice), "Defensive copy should keep original granted tag.");
            Expect(!target.Tags.HasTag(PFTagId.State_DeBuff_Fire), "Defensive copy should ignore mutated granted tag.");
        }

        private void RemoveAllCleansMultipleActiveEffects()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var dynamicEffect = new GameplayEffect(
                "RemoveAllDynamic",
                GameplayEffectLifetime.ForDuration(100f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                },
                grantedTags: new[] { PFTagId.State_Buff });
            var fixedEffect = StackBuff("RemoveAllFixed", GameplayEffectStackingPolicy.Independent());

            var dynamicApply = target.Effects.ApplyToSelf(dynamicEffect, source);
            var fixedApply = target.Effects.ApplyToSelf(fixedEffect, source);

            Expect(dynamicApply.Succeeded, "Dynamic RemoveAll effect should apply.");
            Expect(fixedApply.Succeeded, "Fixed RemoveAll effect should apply.");
            ExpectEqual(target.Effects.ActiveEffectCount, 2, "RemoveAll setup should have two active effects.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 160f, "RemoveAll setup modifiers.");
            Expect(target.Tags.HasTag(PFTagId.State_Buff), "RemoveAll setup should grant tag.");

            target.Effects.RemoveAll();
            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);

            ExpectEqual(target.Effects.ActiveEffectCount, 0, "RemoveAll should clear active effects.");
            ExpectNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f, "RemoveAll should remove modifiers and subscriptions.");
            Expect(!target.Tags.HasTag(PFTagId.State_Buff), "RemoveAll should remove granted tags.");
        }

        private GameplayEffect StackBuff(
            string name,
            GameplayEffectStackingPolicy stacking,
            bool scaleByStackCount = false)
        {
            return new GameplayEffect(
                name,
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply,
                        scaleByStackCount),
                },
                stacking: stacking);
        }

        private GameplayEffect StackBuffWithOnApply(
            string name,
            GameplayEffectStackingPolicy stacking,
            LifecycleCounters counters,
            bool scaleByStackCount = false)
        {
            return new GameplayEffect(
                name,
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply,
                        scaleByStackCount),
                },
                stacking: stacking,
                executions: new[]
                {
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnApply,
                        new CounterExecution(counters, GameplayEffectExecutionPhase.OnApply)),
                });
        }

        private GameplayEffect SourceDynamicStackBuff(
            string name,
            GameplayEffectStackingPolicy stacking)
        {
            return new GameplayEffect(
                name,
                GameplayEffectLifetime.ForDuration(10f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.HP, 0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                },
                stacking: stacking);
        }

        private CombatUnit CreateUnit(string name)
        {
            var gameObject = new GameObject(name);
            objects.Add(gameObject);
            var unit = gameObject.AddComponent<CombatUnit>();
            unit.EnsureInitialized();
            unit.Attributes.AddAttributes(new[]
            {
                PFAttributeRules.HP,
                PFAttributeRules.MaxHP,
            });
            return unit;
        }

        private void CleanupCreatedObjects()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
            {
                if (objects[i] != null)
                {
                    DestroyImmediate(objects[i]);
                }
            }

            objects.Clear();
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void ExpectEqual<T>(T actual, T expected, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(actual, expected))
            {
                throw new InvalidOperationException(
                    $"{message} Expected '{expected}', got '{actual}'.");
            }
        }

        private static void ExpectNearly(float actual, float expected, string message)
        {
            if (!PFGASHelper.IsNearlyEqual(actual, expected))
            {
                throw new InvalidOperationException(
                    $"{message} Expected '{expected}', got '{actual}'.");
            }
        }

        private static void ExpectThrows<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{message} Expected exception '{typeof(TException).Name}', got '{exception.GetType().Name}'.",
                    exception);
            }

            throw new InvalidOperationException(
                $"{message} Expected exception '{typeof(TException).Name}', but no exception was thrown.");
        }

        private sealed class LifecycleCounters
        {
            public int ApplyCount;
            public int PeriodCount;
            public int RemoveCount;
            public int TriggerCount;
            public int DeactivateCount;
            public int TriggerActivationFailures;
            public int TriggerObservedActiveState;
            public int ApplyFailureCount;
            public int RemoveFailureCount;
        }

        private sealed class CounterExecution : IGameplayEffectExecution
        {
            private readonly LifecycleCounters counters;
            private readonly GameplayEffectExecutionPhase phase;

            public CounterExecution(LifecycleCounters counters, GameplayEffectExecutionPhase phase)
            {
                this.counters = counters;
                this.phase = phase;
            }

            public GASResult Execute(GameplayEffectExecutionContext context)
            {
                switch (phase)
                {
                    case GameplayEffectExecutionPhase.OnApply:
                        counters.ApplyCount++;
                        break;
                    case GameplayEffectExecutionPhase.OnPeriod:
                        counters.PeriodCount++;
                        break;
                    case GameplayEffectExecutionPhase.OnRemove:
                        counters.RemoveCount++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return GASResult.Success();
            }
        }

        private sealed class EventCounterTrigger : IGameplayEffectTrigger
        {
            private readonly string eventName;
            private readonly LifecycleCounters counters;
            private Action<GameplayEvent> handler;

            public EventCounterTrigger(string eventName, LifecycleCounters counters)
            {
                this.eventName = eventName;
                this.counters = counters;
            }

            public GASResult Activate(GameplayEffectTriggerContext context)
            {
                handler = _ => counters.TriggerCount++;
                context.Target.GameplayEventBus.Subscribe(eventName, handler);
                return GASResult.Success();
            }

            public void Deactivate(GameplayEffectTriggerContext context)
            {
                if (handler == null)
                {
                    return;
                }

                context.Target.GameplayEventBus.Unsubscribe(eventName, handler);
                handler = null;
                counters.DeactivateCount++;
            }
        }

        private sealed class ObservingActivationTrigger : IGameplayEffectTrigger
        {
            private readonly LifecycleCounters counters;
            private readonly PFTagId expectedTag;
            private readonly float expectedMaxHp;

            public ObservingActivationTrigger(
                LifecycleCounters counters,
                PFTagId expectedTag,
                float expectedMaxHp)
            {
                this.counters = counters;
                this.expectedTag = expectedTag;
                this.expectedMaxHp = expectedMaxHp;
            }

            public GASResult Activate(GameplayEffectTriggerContext context)
            {
                if (!context.Target.Effects.TryGetActiveEffect(context.ActiveEffect.Handle, out var activeEffect) ||
                    !ReferenceEquals(activeEffect, context.ActiveEffect))
                {
                    return GASResult.Fail("MissingActiveState", "Trigger activation could not observe active record.");
                }

                if (!context.Target.Tags.HasTag(expectedTag))
                {
                    return GASResult.Fail("MissingGrantedTag", "Trigger activation could not observe granted tag.");
                }

                if (!PFGASHelper.IsNearlyEqual(
                        context.Target.Attributes.GetCurrentValue(PFAttributeId.MaxHP),
                        expectedMaxHp))
                {
                    return GASResult.Fail("MissingModifierSource", "Trigger activation could not observe ModifierSource.");
                }

                counters.TriggerObservedActiveState++;
                return GASResult.Success();
            }

            public void Deactivate(GameplayEffectTriggerContext context)
            {
            }
        }

        private sealed class FailingTrigger : IGameplayEffectTrigger
        {
            private readonly LifecycleCounters counters;

            public FailingTrigger(LifecycleCounters counters)
            {
                this.counters = counters;
            }

            public GASResult Activate(GameplayEffectTriggerContext context)
            {
                counters.TriggerActivationFailures++;
                return GASResult.Fail("IntentionalTriggerFailure", "Intentional trigger activation failure.");
            }

            public void Deactivate(GameplayEffectTriggerContext context)
            {
            }
        }

        private sealed class FailingSecondActivationTrigger : IGameplayEffectTrigger
        {
            private readonly LifecycleCounters counters;
            private int activationCount;

            public FailingSecondActivationTrigger(LifecycleCounters counters)
            {
                this.counters = counters;
            }

            public GASResult Activate(GameplayEffectTriggerContext context)
            {
                activationCount++;
                if (activationCount < 2)
                {
                    return GASResult.Success();
                }

                counters.TriggerActivationFailures++;
                return GASResult.Fail("IntentionalSecondTriggerFailure", "Intentional second trigger activation failure.");
            }

            public void Deactivate(GameplayEffectTriggerContext context)
            {
            }
        }

        private sealed class FailingExecution : IGameplayEffectExecution
        {
            private readonly LifecycleCounters counters;

            public FailingExecution(LifecycleCounters counters)
            {
                this.counters = counters;
            }

            public GASResult Execute(GameplayEffectExecutionContext context)
            {
                counters.ApplyFailureCount++;
                return GASResult.Fail("IntentionalExecutionFailure", "Intentional execution failure.");
            }
        }

        private sealed class FailingFirstRemoveExecution : IGameplayEffectExecution
        {
            private readonly LifecycleCounters counters;
            private bool hasFailed;

            public FailingFirstRemoveExecution(LifecycleCounters counters)
            {
                this.counters = counters;
            }

            public GASResult Execute(GameplayEffectExecutionContext context)
            {
                if (!hasFailed)
                {
                    hasFailed = true;
                    counters.RemoveFailureCount++;
                    return GASResult.Fail("IntentionalRemoveFailure", "Intentional first OnRemove failure.");
                }

                counters.RemoveCount++;
                return GASResult.Success();
            }
        }
    }
}
