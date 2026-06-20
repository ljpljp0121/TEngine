using NUnit.Framework;
using UnityEngine;

namespace PFGAS.Runtime.Tests
{
    public sealed class PFGASSamplesRuntimeTests
    {
        [Test]
        public void BurningDotShowsPeriodicReevaluationAndTagCleanup()
        {
            var result = PFGASSamples.RunBurningDot();

            Assert.That(result.IceTargetBlocked, Is.True);
            Assert.That(result.BlockedFailureReason, Is.EqualTo("BlockedTargetTags"));
            AssertNearly(result.IceTargetHp, 100f);
            Assert.That(result.ActiveHandleCreated, Is.True);
            Assert.That(result.FireTagWhileActive, Is.True);
            AssertNearly(result.HpAfterApply, 100f);
            AssertNearly(result.HpAfterFirstPeriod, 90f);
            AssertNearly(result.HpAfterSecondPeriod, 70f);
            AssertNearly(result.HpAfterExpiry, 50f);
            Assert.That(result.ActiveEffectCountAfterExpiry, Is.EqualTo(0));
            Assert.That(result.FireTagAfterExpiry, Is.False);
        }

        [Test]
        public void LeadershipAuraShowsSourceDynamicRefreshAndCleanup()
        {
            var result = PFGASSamples.RunLeadershipAura();

            AssertNearly(result.FrontInitialMaxHp, 120f);
            AssertNearly(result.BackInitialMaxHp, 120f);
            AssertNearly(result.FrontBeforeSourceFlush, 120f);
            AssertNearly(result.FrontAfterSourceFlush, 140f);
            AssertNearly(result.BackAfterSourceFlush, 140f);
            AssertNearly(result.FrontAfterRemoveAndSourceChange, 100f);
            AssertNearly(result.BackWhileStillActive, 160f);
            Assert.That(result.FrontActiveEffectCountAfterCleanup, Is.EqualTo(0));
            Assert.That(result.BackActiveEffectCountAfterCleanup, Is.EqualTo(0));
            Assert.That(result.FrontBuffTagAfterCleanup, Is.False);
            Assert.That(result.BackBuffTagAfterCleanup, Is.False);
        }

        [Test]
        public void StackingPoisonShowsScopedStacksOverflowAndCleanup()
        {
            var result = PFGASSamples.RunStackingPoison();

            Assert.That(result.SourceAStackCount, Is.EqualTo(3));
            Assert.That(result.SourceBStackCount, Is.EqualTo(1));
            Assert.That(result.ActiveEffectsAfterSecondSource, Is.EqualTo(2));
            Assert.That(result.OverflowReturnedExisting, Is.True);
            AssertNearly(result.HpAfterSourceAFirstPeriod, 88f);
            AssertNearly(result.HpAfterBothSourcesPeriod, 72f);
            Assert.That(result.PoisonTagWhileActive, Is.True);
            Assert.That(result.ActiveEffectCountAfterCleanup, Is.EqualTo(0));
            Assert.That(result.PoisonTagAfterCleanup, Is.False);
        }

        [Test]
        public void TargetLocalShieldShowsAttributeGraphMagnitudeRecalculationAndCleanup()
        {
            var result = PFGASSamples.RunTargetLocalShield();

            Assert.That(result.ActiveHandleCreated, Is.True);
            AssertNearly(result.HpAfterApply, 50f);
            AssertNearly(result.HpAfterMaxHpChange, 100f);
            AssertNearly(result.HpAfterExpiry, 0f);
            Assert.That(result.ActiveEffectCountAfterExpiry, Is.EqualTo(0));
            Assert.That(result.BuffTagAfterExpiry, Is.False);
        }

        [Test]
        public void LifecycleEventShowsExecutionTriggerEventBusAndCleanup()
        {
            var result = PFGASSamples.RunLifecycleEvent();

            Assert.That(result.ActiveHandleCreated, Is.True);
            Assert.That(result.ApplyCount, Is.EqualTo(1));
            Assert.That(result.RemoveCount, Is.EqualTo(1));
            Assert.That(result.EventCountWhileActive, Is.EqualTo(1));
            Assert.That(result.EventCountAfterCleanup, Is.EqualTo(1));
            Assert.That(result.DeactivateCount, Is.EqualTo(1));
            Assert.That(result.HasEventKeyWhileActive, Is.True);
            Assert.That(result.EventIgnoredAfterCleanup, Is.True);
            Assert.That(result.BuffTagWhileActive, Is.True);
            Assert.That(result.BuffTagAfterCleanup, Is.False);
            Assert.That(result.ActiveEffectCountAfterCleanup, Is.EqualTo(0));
        }

        [Test]
        public void SceneRunnerButtonsSimulateTwoUnitsCastingSkills()
        {
            var gameObject = new GameObject("PFGASSampleScenarioRunnerTest");
            var runner = gameObject.AddComponent<PFGASSampleScenarioRunner>();

            try
            {
                runner.ResetDuel();
                runner.CastBurningAToB();
                runner.CastPoisonBToA();
                runner.CastLeadershipAuraAToB();
                runner.CastShieldA();
                runner.ActivateLifecycleB();
                runner.PublishHitAToB();
                runner.IncreaseAMaxHp();
                runner.TickDuel();

                Assert.That(runner.UnitA, Is.Not.Null);
                Assert.That(runner.UnitB, Is.Not.Null);
                Assert.That(runner.UnitA.Effects.ActiveEffectCount, Is.GreaterThan(0));
                Assert.That(runner.UnitB.Effects.ActiveEffectCount, Is.GreaterThan(0));
                Assert.That(runner.UnitBCounters.EventCount, Is.EqualTo(1));
                Assert.That(
                    runner.UnitB.Attributes.GetCurrentValue(PFAttributeId.MaxHP),
                    Is.EqualTo(130f).Within(0.0001f));

                runner.RemoveAllEffects();

                Assert.That(runner.UnitA.Effects.ActiveEffectCount, Is.EqualTo(0));
                Assert.That(runner.UnitB.Effects.ActiveEffectCount, Is.EqualTo(0));
                Assert.That(runner.UnitA.Tags.HasTag(PFGASTestTagIds.State_Buff), Is.False);
                Assert.That(runner.UnitB.Tags.HasTag(PFGASTestTagIds.State_Buff), Is.False);
            }
            finally
            {
                runner.CleanupDuel();
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneRunnerLifecycleButtonKeepsCountersAcrossMultipleListeners()
        {
            var gameObject = new GameObject("PFGASSampleScenarioRunnerCounterTest");
            var runner = gameObject.AddComponent<PFGASSampleScenarioRunner>();

            try
            {
                runner.ResetDuel();
                runner.ActivateLifecycleA();
                runner.ActivateLifecycleA();
                runner.TickDuel();
                runner.TickDuel();
                runner.TickDuel();
                runner.PublishHitBToA();

                Assert.That(runner.UnitA.Effects.ActiveEffectCount, Is.EqualTo(2));
                Assert.That(runner.UnitACounters.EventCount, Is.EqualTo(2));
            }
            finally
            {
                runner.CleanupDuel();
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneRunnerAutoTickAdvancesPeriodicEffects()
        {
            var gameObject = new GameObject("PFGASSampleScenarioRunnerAutoTickTest");
            var runner = gameObject.AddComponent<PFGASSampleScenarioRunner>();

            try
            {
                runner.ResetDuel();
                var halfSecondDot = new GameplayEffect(
                    "Sample_HalfSecondDot",
                    GameplayEffectLifetime.ForDuration(1f, period: 0.5f),
                    new[]
                    {
                        new GameplayEffectModifierSpec(
                            GameplayEffectModifierPhase.Periodic,
                            PFAttributeId.HP,
                            GEOperation.Add,
                            GameplayEffectMagnitudeSpec.Fixed(-10f),
                            GameplayEffectCapturePolicy.SnapshotOnApply),
                    });
                var applyResult = runner.UnitB.Effects.ApplyToSelf(halfSecondDot, runner.UnitA);

                Assert.That(runner.AutoTickEnabled, Is.True);
                Assert.That(applyResult.Failed, Is.False);
                AssertNearly(runner.UnitB.Attributes.GetBaseValue(PFAttributeId.HP), 100f);

                runner.AdvanceAutoTick(0.49f);

                AssertNearly(runner.UnitB.Attributes.GetBaseValue(PFAttributeId.HP), 100f);
                Assert.That(runner.AutoTickCount, Is.EqualTo(1));
                AssertNearly(runner.ElapsedDuelTime, 0.49f);

                runner.AdvanceAutoTick(0.01f);

                AssertNearly(runner.UnitB.Attributes.GetBaseValue(PFAttributeId.HP), 90f);
                Assert.That(runner.AutoTickCount, Is.EqualTo(2));
                AssertNearly(runner.ElapsedDuelTime, 0.5f);
            }
            finally
            {
                runner.CleanupDuel();
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneRunnerManualTagButtonsExposeHierarchyState()
        {
            var gameObject = new GameObject("PFGASSampleScenarioRunnerTagTest");
            var runner = gameObject.AddComponent<PFGASSampleScenarioRunner>();

            try
            {
                runner.ResetDuel();
                runner.AddFireTagToA();
                runner.AddIceTagToB();
                runner.AddBuffTagToA();
                runner.AddPoisonTagToB();

                Assert.That(runner.IsReady, Is.True);
                Assert.That(runner.UnitA.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Fire), Is.True);
                Assert.That(runner.UnitA.Tags.HasTag(PFGASTestTagIds.State_DeBuff), Is.True);
                Assert.That(runner.UnitA.Tags.HasTag(PFGASTestTagIds.State), Is.True);
                Assert.That(runner.UnitA.Tags.HasTag(PFGASTestTagIds.State_Buff), Is.True);
                Assert.That(runner.UnitB.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Ice), Is.True);
                Assert.That(runner.UnitB.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Du), Is.True);
                Assert.That(runner.UnitB.Tags.HasTag(PFGASTestTagIds.State_DeBuff), Is.True);

                runner.RemoveFireTagFromA();
                runner.RemoveIceTagFromB();

                Assert.That(runner.UnitA.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Fire), Is.False);
                Assert.That(runner.UnitB.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Ice), Is.False);

                runner.ClearAllTags();

                Assert.That(runner.UnitA.Tags.IsEmpty, Is.True);
                Assert.That(runner.UnitB.Tags.IsEmpty, Is.True);
            }
            finally
            {
                runner.CleanupDuel();
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void AssertNearly(float actual, float expected)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
