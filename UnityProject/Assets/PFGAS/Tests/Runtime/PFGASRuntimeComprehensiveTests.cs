using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PFGAS.Runtime.Tests
{
    public sealed class PFGASRuntimeComprehensiveTests
    {
        private const PFAttributeId A = (PFAttributeId)9300000;
        private const PFAttributeId B = (PFAttributeId)9300001;
        private const PFAttributeId C = (PFAttributeId)9300002;

        private readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            CleanupCreatedObjects();
        }

        [Test]
        public void AttributeValueAndModifierValidationRejectsBadInputs()
        {
            Assert.Throws<ArgumentException>(() => new AttributeValue(float.NaN));
            Assert.Throws<ArgumentException>(() => new AttributeValue(1f, minValue: 10f, maxValue: 1f));
            Assert.Throws<ArgumentException>(() => AttributeMagnitude.Fixed(float.PositiveInfinity));
        }

        [Test]
        public void AttributeGraphEnumerationMissingReadsAndEmptyModifierInputAreStable()
        {
            var graph = new AttributeGraph();
            var ids = new List<PFAttributeId> { C };

            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(B, new AttributeValue(20f));
            graph.GetAttributeIds(ids);

            Assert.That(graph.Count, Is.EqualTo(2));
            Assert.That(ids, Is.EquivalentTo(new[] { A, B }));
            Assert.Throws<KeyNotFoundException>(() => graph.GetCurrentValue(C));
            Assert.That(graph.ApplyBaseModifiers(Array.Empty<AttributeModifier>()), Is.Empty);
            Assert.That(graph.RemoveAttribute(C), Is.False);
        }

        [Test]
        public void ApplyBaseModifiersRollsBackAllChangesWhenMagnitudeFails()
        {
            var graph = new AttributeGraph();
            var singleEvents = 0;
            var batchEvents = 0;

            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(B, new AttributeValue(20f));
            graph.AttributeChanged += _ => singleEvents++;
            graph.AttributesChanged += _ => batchEvents++;

            Assert.Throws<InvalidOperationException>(() =>
                graph.ApplyBaseModifiers(new[]
                {
                    new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
                    new AttributeModifier(B, GEOperation.Add, new ThrowingMagnitude()),
                }));

            AssertNearly(graph.GetBaseValue(A), 10f);
            AssertNearly(graph.GetCurrentValue(A), 10f);
            AssertNearly(graph.GetBaseValue(B), 20f);
            AssertNearly(graph.GetCurrentValue(B), 20f);
            Assert.That(singleEvents, Is.EqualTo(0));
            Assert.That(batchEvents, Is.EqualTo(0));
        }

        [Test]
        public void NestedBatchUpdatePublishesOnceAfterOuterDispose()
        {
            var graph = new AttributeGraph();
            var singleEvents = 0;
            var batchEvents = 0;

            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(
                B,
                new AttributeValue(10f),
                new FormulaAttributeEvaluator(
                    new[] { A },
                    (context, _, raw) => raw + context.GetCurrentValue(A)));
            graph.AttributeChanged += _ => singleEvents++;
            graph.AttributesChanged += changes =>
            {
                batchEvents++;
                Assert.That(changes.Length, Is.EqualTo(2));
            };

            using (graph.BatchUpdate())
            {
                using (graph.BatchUpdate())
                {
                    graph.SetBaseValue(A, 2f);
                    graph.SetBaseValue(A, 3f);
                }

                Assert.That(singleEvents, Is.EqualTo(0));
                Assert.That(batchEvents, Is.EqualTo(0));
            }

            Assert.That(batchEvents, Is.EqualTo(1));
            Assert.That(singleEvents, Is.EqualTo(2));
            AssertNearly(graph.GetCurrentValue(A), 3f);
            AssertNearly(graph.GetCurrentValue(B), 13f);
        }

        [Test]
        public void MutationDuringAttributeEventIsRejectedButGraphRemainsUsable()
        {
            var graph = new AttributeGraph();
            var rejectedMutation = false;

            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AttributeChanged += _ =>
            {
                try
                {
                    graph.SetBaseValue(A, 99f);
                }
                catch (InvalidOperationException)
                {
                    rejectedMutation = true;
                }
            };

            graph.SetBaseValue(A, 2f);
            Assert.That(rejectedMutation, Is.True);
            AssertNearly(graph.GetCurrentValue(A), 2f);

            rejectedMutation = false;
            graph.SetBaseValue(A, 3f);
            Assert.That(rejectedMutation, Is.True);
            AssertNearly(graph.GetCurrentValue(A), 3f);
        }

        [Test]
        public void DeepDependencyChainStressKeepsExpectedLeafValue()
        {
            const int count = 128;
            var graph = new AttributeGraph();
            var attributes = new PFAttributeId[count];
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using (graph.BatchUpdate())
            {
                for (var i = 0; i < count; i++)
                {
                    attributes[i] = ToStressAttributeId(i);
                    if (i == 0)
                    {
                        graph.AddAttribute(attributes[i], new AttributeValue(1f));
                        continue;
                    }

                    var dependency = attributes[i - 1];
                    graph.AddAttribute(
                        attributes[i],
                        new AttributeValue(1f),
                        new FormulaAttributeEvaluator(
                            new[] { dependency },
                            (context, _, raw) => raw + context.GetCurrentValue(dependency)));
                }
            }

            for (var i = 2; i < 66; i++)
            {
                graph.SetBaseValue(attributes[0], i);
                AssertNearly(graph.GetCurrentValue(attributes[count - 1]), i + count - 1);
            }

            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(10d));
        }

        [Test]
        public void ModifierSourceAddRemoveStressLeavesNoResidue()
        {
            var graph = new AttributeGraph();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            graph.AddAttribute(A, new AttributeValue(100f));
            graph.AddAttribute(B, new AttributeValue(25f));

            for (var i = 0; i < 250; i++)
            {
                var handle = graph.AddModifierSource(new ModifierSource(
                    "stress-" + i,
                    new[]
                    {
                        new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(1f)),
                        new AttributeModifier(
                            A,
                            GEOperation.Add,
                            AttributeMagnitude.Multiply(
                                AttributeMagnitude.Attribute(B),
                                AttributeMagnitude.Fixed(0.1f))),
                    }));

                AssertNearly(graph.GetCurrentValue(A), 103.5f);
                Assert.That(graph.RemoveModifierSource(handle), Is.True);
                AssertNearly(graph.GetCurrentValue(A), 100f);
            }

            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(10d));
        }

        [Test]
        public void GameplayTagAggregatorCountsLooseAndSourceTags()
        {
            var tags = new GameplayTagAggregator();
            var sourceA = new object();
            var sourceB = new object();

            Assert.That(tags.AddLooseTag(PFTagId.State_DeBuff_Fire), Is.True);
            Assert.That(tags.AddLooseTag(PFTagId.State_DeBuff_Fire), Is.False);
            Assert.That(tags.HasTag(PFTagId.State_DeBuff), Is.True);
            Assert.That(tags.HasExactTag(PFTagId.State_DeBuff_Fire), Is.True);

            tags.AddSourceTags(sourceA, PFTagId.State_DeBuff_Ice);
            tags.AddSourceTags(sourceB, PFTagId.State_DeBuff_Ice);
            Assert.That(tags.SourceCount, Is.EqualTo(2));
            Assert.That(tags.HasExactTag(PFTagId.State_DeBuff_Ice), Is.True);

            tags.RemoveSourceTags(sourceA);
            Assert.That(tags.HasExactTag(PFTagId.State_DeBuff_Ice), Is.True);
            tags.RemoveSourceTags(sourceB);
            Assert.That(tags.HasExactTag(PFTagId.State_DeBuff_Ice), Is.False);

            tags.RemoveLooseTag(PFTagId.State_DeBuff_Fire);
            Assert.That(tags.IsEmpty, Is.True);
        }

        [Test]
        public void GameplayEffectConstructorAndLifetimeValidateBoundaryCases()
        {
            var effect = new GameplayEffect(" ", GameplayEffectLifetime.Instant);

            Assert.That(effect.Name, Is.EqualTo(nameof(GameplayEffect)));
            Assert.That(effect.Modifiers, Is.Empty);
            Assert.That(effect.GrantedTags, Is.Empty);
            Assert.That(effect.Executions, Is.Empty);
            Assert.That(effect.Triggers, Is.Empty);
            Assert.That(effect.Stacking.Mode, Is.EqualTo(GameplayEffectStackingMode.Independent));

            Assert.Throws<ArgumentException>(() =>
                new GameplayEffectLifetime(GameplayEffectDurationPolicy.Duration, float.NaN));
            Assert.Throws<ArgumentException>(() =>
                new GameplayEffectLifetime(GameplayEffectDurationPolicy.Duration, 1f, float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GameplayEffectLifetime(GameplayEffectDurationPolicy.Duration, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GameplayEffectLifetime(GameplayEffectDurationPolicy.Instant, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameplayEffectStackingPolicy.Stack(0));
        }

        [Test]
        public void GameplayEffectContainerRejectsInvalidTickDeltas()
        {
            var target = CreateUnit("Target");

            Assert.Throws<ArgumentException>(() => target.Effects.Tick(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => target.Effects.Tick(-0.001f));
            Assert.DoesNotThrow(() => target.Effects.Tick(0f));
        }

        [Test]
        public void InstantMultiAttributeModifiersAreAtomicAndClampCurrentValues()
        {
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "InstantCombo",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-50f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(100f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            var result = target.Effects.ApplyToSelf(effect, target);

            AssertSuccess(result);
            AssertNearly(target.Attributes.GetBaseValue(PFAttributeId.MaxHP), 50f);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 50f);
            AssertNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 200f);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 50f);
            Assert.That(result.Value.AttributeChanges.Count, Is.EqualTo(2));
        }

        [Test]
        public void InstantPrepareFailureLeavesTargetUntouched()
        {
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "MissingAttributeInstant",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(50f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        C,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(1f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            var result = target.Effects.ApplyToSelf(effect, target);

            Assert.That(result.Failed, Is.True);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 100f);
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(0));
        }

        [Test]
        public void PeriodicTickCatchupExecutesAllDuePeriodsAndExpires()
        {
            var target = CreateUnit("Target");
            var effect = PeriodicDamage("Catchup", 3f, 1f, -5f);

            var result = target.Effects.ApplyToSelf(effect, target);
            AssertSuccess(result);

            target.Effects.Tick(2.5f);
            AssertNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 90f);
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(1));

            target.Effects.Tick(0.5f);
            AssertNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 85f);
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(0));
        }

        [Test]
        public void ExecutePeriodicOnApplyRunsImmediatelyThenOnSchedule()
        {
            var target = CreateUnit("Target");
            var effect = PeriodicDamage("ImmediatePeriodic", 2.5f, 1f, -5f, executeOnApply: true);

            var result = target.Effects.ApplyToSelf(effect, target);
            AssertSuccess(result);
            AssertNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 95f);

            target.Effects.Tick(1f);
            AssertNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 90f);
            target.Effects.Tick(1.5f);
            AssertNearly(target.Attributes.GetBaseValue(PFAttributeId.HP), 85f);
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(0));
        }

        [Test]
        public void InfiniteEffectsStayActiveUntilRemovedAndCleanupState()
        {
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "InfiniteBuff",
                GameplayEffectLifetime.Infinite(),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                grantedTags: new[] { PFTagId.State_Buff });

            var result = target.Effects.ApplyToSelf(effect, target);
            AssertSuccess(result);
            target.Effects.Tick(10000f);

            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(1));
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f);
            Assert.That(target.Tags.HasTag(PFTagId.State_Buff), Is.True);

            Assert.That(target.Effects.Remove(result.Value.Handle).Succeeded, Is.True);
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(0));
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f);
            Assert.That(target.Tags.HasTag(PFTagId.State_Buff), Is.False);
        }

        [Test]
        public void OverlappingGrantedTagsStayUntilLastSourceIsRemoved()
        {
            var target = CreateUnit("Target");
            var first = TagGrant("FirstGrant");
            var second = TagGrant("SecondGrant");

            var firstApply = target.Effects.ApplyToSelf(first, target);
            var secondApply = target.Effects.ApplyToSelf(second, target);

            AssertSuccess(firstApply);
            AssertSuccess(secondApply);
            Assert.That(target.Tags.HasTag(PFTagId.State_DeBuff_Fire), Is.True);

            Assert.That(target.Effects.Remove(firstApply.Value.Handle).Succeeded, Is.True);
            Assert.That(target.Tags.HasTag(PFTagId.State_DeBuff_Fire), Is.True);

            Assert.That(target.Effects.Remove(secondApply.Value.Handle).Succeeded, Is.True);
            Assert.That(target.Tags.HasTag(PFTagId.State_DeBuff_Fire), Is.False);
        }

        [Test]
        public void RemoveInvalidExpiredAndForeignHandlesReturnFailuresWithoutMutation()
        {
            var target = CreateUnit("Target");
            var other = CreateUnit("Other");
            var effect = new GameplayEffect("ShortBuff", GameplayEffectLifetime.ForDuration(0.5f));

            Assert.That(target.Effects.Remove(GameplayEffectHandle.Invalid).Failed, Is.True);

            var targetApply = target.Effects.ApplyToSelf(effect, target);
            var otherApply = other.Effects.ApplyToSelf(effect, other);
            AssertSuccess(targetApply);
            AssertSuccess(otherApply);

            Assert.That(target.Effects.Remove(otherApply.Value.Handle).Failed, Is.True);
            target.Effects.Tick(0.5f);
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(0));
            Assert.That(target.Effects.Remove(targetApply.Value.Handle).Failed, Is.True);
        }

        [Test]
        public void TargetLocalMagnitudeDependenciesTrackWhileActiveAndCleanupOnExpire()
        {
            var target = CreateUnit("Target");
            target.Attributes.SetBaseValue(PFAttributeId.HP, 10f);
            var effect = new GameplayEffect(
                "TargetLocalDynamic",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.FromTargetMagnitude(
                            AttributeMagnitude.Multiply(
                                AttributeMagnitude.Attribute(PFAttributeId.MaxHP),
                                AttributeMagnitude.Fixed(0.1f))),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            var result = target.Effects.ApplyToSelf(effect, target);
            AssertSuccess(result);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 20f);

            target.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 30f);

            target.Effects.Tick(5f);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.HP), 10f);
        }

        [Test]
        public void SnapshotStackScalingUsesFirstCapturedSourceValue()
        {
            var source = CreateUnit("Source");
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "SnapshotStack",
                GameplayEffectLifetime.ForDuration(30f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.1f),
                        GameplayEffectCapturePolicy.SnapshotOnApply,
                        scaleByStackCount: true),
                },
                stacking: GameplayEffectStackingPolicy.Stack(3));

            AssertSuccess(target.Effects.ApplyToSelf(effect, source));
            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
            AssertSuccess(target.Effects.ApplyToSelf(effect, source));
            source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 300f);
            AssertSuccess(target.Effects.ApplyToSelf(effect, source));

            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(1));
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 130f);
        }

        [Test]
        public void StackingScopeBySourceAndTargetKeepsSeparateSourceStacks()
        {
            var sourceA = CreateUnit("SourceA");
            var sourceB = CreateUnit("SourceB");
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "SourceScopedStack",
                GameplayEffectLifetime.ForDuration(30f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(10f),
                        GameplayEffectCapturePolicy.SnapshotOnApply,
                        scaleByStackCount: true),
                },
                stacking: GameplayEffectStackingPolicy.Stack(
                    2,
                    GameplayEffectStackingScope.BySourceAndTarget));

            AssertSuccess(target.Effects.ApplyToSelf(effect, sourceA));
            AssertSuccess(target.Effects.ApplyToSelf(effect, sourceA));
            AssertSuccess(target.Effects.ApplyToSelf(effect, sourceB));

            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(2));
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 130f);
        }

        [Test]
        public void DynamicSourceManyTargetsCoalesceUntilTickAndCleanSubscriptions()
        {
            const int targetCount = 12;
            var source = CreateUnit("Source");
            var targets = new List<CombatUnit>();
            var effect = new GameplayEffect(
                "ManyTargetDynamic",
                GameplayEffectLifetime.ForDuration(100f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.1f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            for (var i = 0; i < targetCount; i++)
            {
                var target = CreateUnit("Target" + i);
                targets.Add(target);
                AssertSuccess(target.Effects.ApplyToSelf(effect, source));
                AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f);
            }

            for (var i = 0; i < 10; i++)
            {
                source.Attributes.AddBaseValue(PFAttributeId.MaxHP, 1f);
            }

            for (var i = 0; i < targets.Count; i++)
            {
                AssertNearly(targets[i].Attributes.GetCurrentValue(PFAttributeId.MaxHP), 110f);
                targets[i].Effects.Tick(0f);
                AssertNearly(targets[i].Attributes.GetCurrentValue(PFAttributeId.MaxHP), 111f);
                targets[i].Effects.RemoveAll();
                AssertNearly(targets[i].Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f);
            }

            source.Attributes.AddBaseValue(PFAttributeId.MaxHP, 100f);
            for (var i = 0; i < targets.Count; i++)
            {
                targets[i].Effects.Tick(0f);
                AssertNearly(targets[i].Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f);
            }
        }

        [Test]
        public void TriggerActivationFailureRollsBackTagsModifiersAndActiveRecord()
        {
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "FailingTriggerRollback",
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
                triggers: new[] { new GameplayEffectTriggerSpec(new FailingTrigger()) });

            var result = target.Effects.ApplyToSelf(effect, target);

            Assert.That(result.Failed, Is.True);
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(0));
            Assert.That(target.Tags.HasTag(PFTagId.State_Buff), Is.False);
            AssertNearly(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f);
        }

        [Test]
        public void HighVolumeInstantApplyPerformanceSmokeKeepsValuesCorrect()
        {
            const int iterations = 500;
            var target = CreateUnit("Target");
            var effect = new GameplayEffect(
                "InstantPerfSmoke",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-0.05f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                AssertSuccess(target.Effects.ApplyToSelf(effect, target));
            }

            stopwatch.Stop();
            Assert.That(target.Attributes.GetBaseValue(PFAttributeId.HP), Is.EqualTo(75f).Within(0.01f));
            Assert.That(target.Effects.ActiveEffectCount, Is.EqualTo(0));
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(10d));
        }

        [Test]
        public void DurationEffectChurnPerformanceSmokeCleansEveryTarget()
        {
            const int targetCount = 10;
            const int effectsPerTarget = 20;
            var source = CreateUnit("Source");
            var targets = new List<CombatUnit>();
            var effect = new GameplayEffect(
                "DurationChurn",
                GameplayEffectLifetime.ForDuration(1000f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(1f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                stacking: GameplayEffectStackingPolicy.Independent());
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (var i = 0; i < targetCount; i++)
            {
                targets.Add(CreateUnit("ChurnTarget" + i));
            }

            for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                for (var effectIndex = 0; effectIndex < effectsPerTarget; effectIndex++)
                {
                    AssertSuccess(targets[targetIndex].Effects.ApplyToSelf(effect, source));
                }

                AssertNearly(targets[targetIndex].Attributes.GetCurrentValue(PFAttributeId.MaxHP), 120f);
            }

            for (var i = 0; i < targets.Count; i++)
            {
                targets[i].Effects.RemoveAll();
                AssertNearly(targets[i].Attributes.GetCurrentValue(PFAttributeId.MaxHP), 100f);
                Assert.That(targets[i].Effects.ActiveEffectCount, Is.EqualTo(0));
            }

            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(10d));
        }

        private static GameplayEffect PeriodicDamage(
            string name,
            float duration,
            float period,
            float amount,
            bool executeOnApply = false)
        {
            return new GameplayEffect(
                name,
                GameplayEffectLifetime.ForDuration(duration, period, executeOnApply),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(amount),
                        GameplayEffectCapturePolicy.ReevaluateOnPeriod),
                });
        }

        private static GameplayEffect TagGrant(string name)
        {
            return new GameplayEffect(
                name,
                GameplayEffectLifetime.ForDuration(10f),
                grantedTags: new[] { PFTagId.State_DeBuff_Fire });
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
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }

            objects.Clear();
        }

        private static PFAttributeId ToStressAttributeId(int index)
        {
            return (PFAttributeId)(9310000 + index);
        }

        private static void AssertSuccess(GASResult result)
        {
            Assert.That(result.Succeeded, Is.True, result.Failure.ToString());
        }

        private static void AssertSuccess<T>(GASResult<T> result)
        {
            Assert.That(result.Succeeded, Is.True, result.Failure.ToString());
        }

        private static void AssertNearly(float actual, float expected)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));
        }

        private sealed class ThrowingMagnitude : IAttributeMagnitude
        {
            public IReadOnlyList<PFAttributeId> Dependencies => Array.Empty<PFAttributeId>();

            public float Evaluate(AttributeGraphContext context)
            {
                throw new InvalidOperationException("Intentional test magnitude failure.");
            }
        }

        private sealed class FailingTrigger : IGameplayEffectTrigger
        {
            public GASResult Activate(GameplayEffectTriggerContext context)
            {
                return GASResult.Fail("IntentionalTriggerFailure", "Intentional test trigger failure.");
            }

            public void Deactivate(GameplayEffectTriggerContext context)
            {
            }
        }
    }
}
