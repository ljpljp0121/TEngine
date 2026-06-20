using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFGAS.Runtime.Tests
{
    public sealed class PFGASSampleUnitFactory : IDisposable
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        public CombatUnit CreateUnit(string name, float hp = 100f, float maxHp = 100f)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);

            var unit = gameObject.AddComponent<CombatUnit>();
            unit.EnsureInitialized();
            unit.Attributes.AddAttributes(new[]
            {
                PFAttributeRules.HP,
                PFAttributeRules.MaxHP,
            });
            unit.Attributes.SetBaseValue(PFAttributeId.MaxHP, maxHp);
            unit.Attributes.SetBaseValue(PFAttributeId.HP, hp);
            return unit;
        }

        public void Dispose()
        {
            CleanupCreatedObjects();
        }

        public void CleanupCreatedObjects()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }
    }

    public static class PFGASSampleEffects
    {
        // 灼烧：Duration + Period，每次周期重新读取 source MaxHP，并给目标挂上火焰 Tag。
        public static GameplayEffect CreateBurningDot()
        {
            return new GameplayEffect(
                "Sample_BurningDot",
                GameplayEffectLifetime.ForDuration(3f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, -0.1f),
                        GameplayEffectCapturePolicy.ReevaluateOnPeriod),
                },
                tags: new GameplayEffectTagRequirements(
                    blockedTargetTags: new[] { PFGASTestTagIds.State_DeBuff_Ice }),
                grantedTags: new[] { PFGASTestTagIds.State_DeBuff_Fire });
        }

        // 队长光环：Ongoing modifier 动态读取 source MaxHP，适合一名 source 增益多个目标。
        public static GameplayEffect CreateLeadershipAura()
        {
            return new GameplayEffect(
                "Sample_LeadershipAura",
                GameplayEffectLifetime.Infinite(),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.2f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                },
                grantedTags: new[] { PFGASTestTagIds.State_Buff });
        }

        // 毒层：同一 source/target 叠层到上限，周期伤害按 StackCount 缩放，溢出明确忽略。
        public static GameplayEffect CreateStackingPoison()
        {
            return new GameplayEffect(
                "Sample_StackingPoison",
                GameplayEffectLifetime.ForDuration(4f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-4f),
                        GameplayEffectCapturePolicy.ReevaluateOnPeriod,
                        scaleByStackCount: true),
                },
                stacking: GameplayEffectStackingPolicy.Stack(
                    3,
                    GameplayEffectStackingScope.BySourceAndTarget,
                    overflowPolicy: GameplayEffectOverflowPolicy.Ignore),
                grantedTags: new[] { PFGASTestTagIds.State_DeBuff_Du });
        }

        // 生命护盾：target-local magnitude 进入 AttributeGraph，MaxHP 变化会自动重算护盾值。
        public static GameplayEffect CreateTargetLocalShield()
        {
            return new GameplayEffect(
                "Sample_TargetLocalShield",
                GameplayEffectLifetime.ForDuration(5f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.FromTargetMagnitude(
                            AttributeMagnitude.Attribute(PFAttributeId.MaxHP),
                            0.5f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                },
                grantedTags: new[] { PFGASTestTagIds.State_Buff });
        }

        // 生命周期：Execution 记录 apply/remove，Trigger 只在 active 期间订阅 GameplayEventBus。
        public static GameplayEffect CreateLifecycleEvent(
            PFGASSampleLifecycleCounters counters,
            string eventName = PFGASSamples.LifecycleEventName)
        {
            return CreateLifecycleEvent(
                counters,
                eventName,
                GameplayEffectLifetime.ForDuration(2f));
        }

        // 场景按钮用的持续监听版本：不会自动过期，方便手动点击命中观察事件计数。
        public static GameplayEffect CreatePersistentLifecycleEvent(
            PFGASSampleLifecycleCounters counters,
            string eventName = PFGASSamples.LifecycleEventName)
        {
            return CreateLifecycleEvent(
                counters,
                eventName,
                GameplayEffectLifetime.Infinite());
        }

        private static GameplayEffect CreateLifecycleEvent(
            PFGASSampleLifecycleCounters counters,
            string eventName,
            GameplayEffectLifetime lifetime)
        {
            return new GameplayEffect(
                "Sample_LifecycleEvent",
                lifetime,
                grantedTags: new[] { PFGASTestTagIds.State_Buff },
                executions: new[]
                {
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnApply,
                        new PFGASSampleCountingExecution(counters, GameplayEffectExecutionPhase.OnApply)),
                    new GameplayEffectExecutionSpec(
                        GameplayEffectExecutionPhase.OnRemove,
                        new PFGASSampleCountingExecution(counters, GameplayEffectExecutionPhase.OnRemove)),
                },
                triggers: new[]
                {
                    new GameplayEffectTriggerSpec(new PFGASSampleEventTrigger(eventName, counters)),
                });
        }
    }

    public static class PFGASSamples
    {
        public const string LifecycleEventName = "PFGAS.Sample.Hit";

        public static BurningDotSampleResult RunBurningDot()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleBurningSource");
                var target = factory.CreateUnit("SampleBurningTarget");
                var iceTarget = factory.CreateUnit("SampleBurningIceTarget");
                var burning = PFGASSampleEffects.CreateBurningDot();

                iceTarget.Tags.AddLooseTag(PFGASTestTagIds.State_DeBuff_Ice);
                var blockedApply = iceTarget.Effects.ApplyToSelf(burning, source);

                var apply = RequireSuccess(
                    target.Effects.ApplyToSelf(burning, source),
                    "Burning sample apply failed.");

                var hpAfterApply = target.Attributes.GetBaseValue(PFAttributeId.HP);
                var fireTagWhileActive = target.Tags.HasTag(PFGASTestTagIds.State_DeBuff_Fire);

                target.Effects.Tick(1f);
                var hpAfterFirstPeriod = target.Attributes.GetBaseValue(PFAttributeId.HP);

                source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
                target.Effects.Tick(1f);
                var hpAfterSecondPeriod = target.Attributes.GetBaseValue(PFAttributeId.HP);

                target.Effects.Tick(1f);
                return new BurningDotSampleResult(
                    blockedApply.Failed,
                    blockedApply.Failure.Reason,
                    iceTarget.Attributes.GetBaseValue(PFAttributeId.HP),
                    hpAfterApply,
                    hpAfterFirstPeriod,
                    hpAfterSecondPeriod,
                    target.Attributes.GetBaseValue(PFAttributeId.HP),
                    fireTagWhileActive,
                    target.Tags.HasTag(PFGASTestTagIds.State_DeBuff_Fire),
                    target.Effects.ActiveEffectCount,
                    apply.Handle.IsValid);
            }
        }

        public static LeadershipAuraSampleResult RunLeadershipAura()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleLeader");
                var frontTarget = factory.CreateUnit("SampleAuraFrontTarget");
                var backTarget = factory.CreateUnit("SampleAuraBackTarget");
                var aura = PFGASSampleEffects.CreateLeadershipAura();

                var frontApply = RequireSuccess(
                    frontTarget.Effects.ApplyToSelf(aura, source),
                    "Leadership aura front apply failed.");
                var backApply = RequireSuccess(
                    backTarget.Effects.ApplyToSelf(aura, source),
                    "Leadership aura back apply failed.");

                var frontInitialMaxHp = frontTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP);
                var backInitialMaxHp = backTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP);

                source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
                var frontBeforeFlush = frontTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP);
                frontTarget.Effects.Tick(0f);
                backTarget.Effects.Tick(0f);
                var frontAfterFlush = frontTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP);
                var backAfterFlush = backTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP);

                RequireSuccess(
                    frontTarget.Effects.Remove(frontApply.Handle),
                    "Leadership aura front remove failed.");
                source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 300f);
                frontTarget.Effects.Tick(0f);
                backTarget.Effects.Tick(0f);

                var frontAfterRemoveAndSourceChange =
                    frontTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP);
                var backWhileStillActive =
                    backTarget.Attributes.GetCurrentValue(PFAttributeId.MaxHP);

                RequireSuccess(
                    backTarget.Effects.Remove(backApply.Handle),
                    "Leadership aura back remove failed.");

                return new LeadershipAuraSampleResult(
                    frontInitialMaxHp,
                    backInitialMaxHp,
                    frontBeforeFlush,
                    frontAfterFlush,
                    backAfterFlush,
                    frontAfterRemoveAndSourceChange,
                    backWhileStillActive,
                    frontTarget.Effects.ActiveEffectCount,
                    backTarget.Effects.ActiveEffectCount,
                    frontTarget.Tags.HasTag(PFGASTestTagIds.State_Buff),
                    backTarget.Tags.HasTag(PFGASTestTagIds.State_Buff));
            }
        }

        public static StackingPoisonSampleResult RunStackingPoison()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var sourceA = factory.CreateUnit("SamplePoisonSourceA");
                var sourceB = factory.CreateUnit("SamplePoisonSourceB");
                var target = factory.CreateUnit("SamplePoisonTarget");
                var poison = PFGASSampleEffects.CreateStackingPoison();

                var firstApply = RequireSuccess(
                    target.Effects.ApplyToSelf(poison, sourceA),
                    "Poison first stack failed.");
                RequireSuccess(target.Effects.ApplyToSelf(poison, sourceA), "Poison second stack failed.");
                RequireSuccess(target.Effects.ApplyToSelf(poison, sourceA), "Poison third stack failed.");
                var overflowApply = RequireSuccess(
                    target.Effects.ApplyToSelf(poison, sourceA),
                    "Poison overflow apply failed.");

                target.Effects.TryGetActiveEffect(firstApply.Handle, out var sourceAActive);
                var sourceAStackCount = sourceAActive != null ? sourceAActive.StackCount : 0;
                var overflowReturnedExisting = overflowApply.Handle.Equals(firstApply.Handle);

                target.Effects.Tick(1f);
                var hpAfterSourceAFirstPeriod = target.Attributes.GetBaseValue(PFAttributeId.HP);

                var sourceBApply = RequireSuccess(
                    target.Effects.ApplyToSelf(poison, sourceB),
                    "Poison source B apply failed.");
                target.Effects.TryGetActiveEffect(sourceBApply.Handle, out var sourceBActive);
                var sourceBStackCount = sourceBActive != null ? sourceBActive.StackCount : 0;
                var activeEffectsAfterSecondSource = target.Effects.ActiveEffectCount;

                target.Effects.Tick(1f);
                var hpAfterBothSourcesPeriod = target.Attributes.GetBaseValue(PFAttributeId.HP);
                var poisonTagWhileActive = target.Tags.HasTag(PFGASTestTagIds.State_DeBuff_Du);

                target.Effects.RemoveAll();

                return new StackingPoisonSampleResult(
                    sourceAStackCount,
                    sourceBStackCount,
                    activeEffectsAfterSecondSource,
                    overflowReturnedExisting,
                    hpAfterSourceAFirstPeriod,
                    hpAfterBothSourcesPeriod,
                    poisonTagWhileActive,
                    target.Tags.HasTag(PFGASTestTagIds.State_DeBuff_Du),
                    target.Effects.ActiveEffectCount);
            }
        }

        public static TargetLocalShieldSampleResult RunTargetLocalShield()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleShieldSource");
                var target = factory.CreateUnit("SampleShieldTarget", hp: 0f, maxHp: 100f);
                var shield = PFGASSampleEffects.CreateTargetLocalShield();

                var apply = RequireSuccess(
                    target.Effects.ApplyToSelf(shield, source),
                    "Target local shield apply failed.");

                var hpAfterApply = target.Attributes.GetCurrentValue(PFAttributeId.HP);
                target.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
                var hpAfterMaxHpChange = target.Attributes.GetCurrentValue(PFAttributeId.HP);

                target.Effects.Tick(5f);

                return new TargetLocalShieldSampleResult(
                    hpAfterApply,
                    hpAfterMaxHpChange,
                    target.Attributes.GetCurrentValue(PFAttributeId.HP),
                    target.Effects.ActiveEffectCount,
                    target.Tags.HasTag(PFGASTestTagIds.State_Buff),
                    apply.Handle.IsValid);
            }
        }

        public static LifecycleEventSampleResult RunLifecycleEvent()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleLifecycleSource");
                var target = factory.CreateUnit("SampleLifecycleTarget");
                var counters = new PFGASSampleLifecycleCounters();
                var effect = PFGASSampleEffects.CreateLifecycleEvent(counters);

                var apply = RequireSuccess(
                    target.Effects.ApplyToSelf(effect, source),
                    "Lifecycle event apply failed.");

                target.GameplayEventBus.Publish(LifecycleEventName, source, target);
                var eventCountWhileActive = counters.EventCount;
                var hasEventKeyWhileActive = target.GameplayEventBus.HasEvent(LifecycleEventName);
                var hasGrantedTagWhileActive = target.Tags.HasTag(PFGASTestTagIds.State_Buff);

                target.Effects.Tick(2f);
                var eventCountBeforeCleanupPublish = counters.EventCount;
                target.GameplayEventBus.Publish(LifecycleEventName, source, target);

                return new LifecycleEventSampleResult(
                    counters.ApplyCount,
                    counters.RemoveCount,
                    eventCountWhileActive,
                    counters.EventCount,
                    counters.DeactivateCount,
                    hasEventKeyWhileActive,
                    counters.EventCount == eventCountBeforeCleanupPublish,
                    hasGrantedTagWhileActive,
                    target.Tags.HasTag(PFGASTestTagIds.State_Buff),
                    target.Effects.ActiveEffectCount,
                    apply.Handle.IsValid);
            }
        }

        private static GameplayEffectApplyResult RequireSuccess(
            GASResult<GameplayEffectApplyResult> result,
            string message)
        {
            if (result.Failed)
            {
                throw new InvalidOperationException(message + " " + result.Failure);
            }

            return result.Value;
        }

        private static void RequireSuccess(GASResult result, string message)
        {
            if (result.Failed)
            {
                throw new InvalidOperationException(message + " " + result.Failure);
            }
        }
    }



    public readonly struct BurningDotSampleResult
    {
        public BurningDotSampleResult(
            bool iceTargetBlocked,
            string blockedFailureReason,
            float iceTargetHp,
            float hpAfterApply,
            float hpAfterFirstPeriod,
            float hpAfterSecondPeriod,
            float hpAfterExpiry,
            bool fireTagWhileActive,
            bool fireTagAfterExpiry,
            int activeEffectCountAfterExpiry,
            bool activeHandleCreated)
        {
            IceTargetBlocked = iceTargetBlocked;
            BlockedFailureReason = blockedFailureReason;
            IceTargetHp = iceTargetHp;
            HpAfterApply = hpAfterApply;
            HpAfterFirstPeriod = hpAfterFirstPeriod;
            HpAfterSecondPeriod = hpAfterSecondPeriod;
            HpAfterExpiry = hpAfterExpiry;
            FireTagWhileActive = fireTagWhileActive;
            FireTagAfterExpiry = fireTagAfterExpiry;
            ActiveEffectCountAfterExpiry = activeEffectCountAfterExpiry;
            ActiveHandleCreated = activeHandleCreated;
        }

        public bool IceTargetBlocked { get; }
        public string BlockedFailureReason { get; }
        public float IceTargetHp { get; }
        public float HpAfterApply { get; }
        public float HpAfterFirstPeriod { get; }
        public float HpAfterSecondPeriod { get; }
        public float HpAfterExpiry { get; }
        public bool FireTagWhileActive { get; }
        public bool FireTagAfterExpiry { get; }
        public int ActiveEffectCountAfterExpiry { get; }
        public bool ActiveHandleCreated { get; }
    }

    public readonly struct LeadershipAuraSampleResult
    {
        public LeadershipAuraSampleResult(
            float frontInitialMaxHp,
            float backInitialMaxHp,
            float frontBeforeSourceFlush,
            float frontAfterSourceFlush,
            float backAfterSourceFlush,
            float frontAfterRemoveAndSourceChange,
            float backWhileStillActive,
            int frontActiveEffectCountAfterCleanup,
            int backActiveEffectCountAfterCleanup,
            bool frontBuffTagAfterCleanup,
            bool backBuffTagAfterCleanup)
        {
            FrontInitialMaxHp = frontInitialMaxHp;
            BackInitialMaxHp = backInitialMaxHp;
            FrontBeforeSourceFlush = frontBeforeSourceFlush;
            FrontAfterSourceFlush = frontAfterSourceFlush;
            BackAfterSourceFlush = backAfterSourceFlush;
            FrontAfterRemoveAndSourceChange = frontAfterRemoveAndSourceChange;
            BackWhileStillActive = backWhileStillActive;
            FrontActiveEffectCountAfterCleanup = frontActiveEffectCountAfterCleanup;
            BackActiveEffectCountAfterCleanup = backActiveEffectCountAfterCleanup;
            FrontBuffTagAfterCleanup = frontBuffTagAfterCleanup;
            BackBuffTagAfterCleanup = backBuffTagAfterCleanup;
        }

        public float FrontInitialMaxHp { get; }
        public float BackInitialMaxHp { get; }
        public float FrontBeforeSourceFlush { get; }
        public float FrontAfterSourceFlush { get; }
        public float BackAfterSourceFlush { get; }
        public float FrontAfterRemoveAndSourceChange { get; }
        public float BackWhileStillActive { get; }
        public int FrontActiveEffectCountAfterCleanup { get; }
        public int BackActiveEffectCountAfterCleanup { get; }
        public bool FrontBuffTagAfterCleanup { get; }
        public bool BackBuffTagAfterCleanup { get; }
    }

    public readonly struct StackingPoisonSampleResult
    {
        public StackingPoisonSampleResult(
            int sourceAStackCount,
            int sourceBStackCount,
            int activeEffectsAfterSecondSource,
            bool overflowReturnedExisting,
            float hpAfterSourceAFirstPeriod,
            float hpAfterBothSourcesPeriod,
            bool poisonTagWhileActive,
            bool poisonTagAfterCleanup,
            int activeEffectCountAfterCleanup)
        {
            SourceAStackCount = sourceAStackCount;
            SourceBStackCount = sourceBStackCount;
            ActiveEffectsAfterSecondSource = activeEffectsAfterSecondSource;
            OverflowReturnedExisting = overflowReturnedExisting;
            HpAfterSourceAFirstPeriod = hpAfterSourceAFirstPeriod;
            HpAfterBothSourcesPeriod = hpAfterBothSourcesPeriod;
            PoisonTagWhileActive = poisonTagWhileActive;
            PoisonTagAfterCleanup = poisonTagAfterCleanup;
            ActiveEffectCountAfterCleanup = activeEffectCountAfterCleanup;
        }

        public int SourceAStackCount { get; }
        public int SourceBStackCount { get; }
        public int ActiveEffectsAfterSecondSource { get; }
        public bool OverflowReturnedExisting { get; }
        public float HpAfterSourceAFirstPeriod { get; }
        public float HpAfterBothSourcesPeriod { get; }
        public bool PoisonTagWhileActive { get; }
        public bool PoisonTagAfterCleanup { get; }
        public int ActiveEffectCountAfterCleanup { get; }
    }

    public readonly struct TargetLocalShieldSampleResult
    {
        public TargetLocalShieldSampleResult(
            float hpAfterApply,
            float hpAfterMaxHpChange,
            float hpAfterExpiry,
            int activeEffectCountAfterExpiry,
            bool buffTagAfterExpiry,
            bool activeHandleCreated)
        {
            HpAfterApply = hpAfterApply;
            HpAfterMaxHpChange = hpAfterMaxHpChange;
            HpAfterExpiry = hpAfterExpiry;
            ActiveEffectCountAfterExpiry = activeEffectCountAfterExpiry;
            BuffTagAfterExpiry = buffTagAfterExpiry;
            ActiveHandleCreated = activeHandleCreated;
        }

        public float HpAfterApply { get; }
        public float HpAfterMaxHpChange { get; }
        public float HpAfterExpiry { get; }
        public int ActiveEffectCountAfterExpiry { get; }
        public bool BuffTagAfterExpiry { get; }
        public bool ActiveHandleCreated { get; }
    }

    public readonly struct LifecycleEventSampleResult
    {
        public LifecycleEventSampleResult(
            int applyCount,
            int removeCount,
            int eventCountWhileActive,
            int eventCountAfterCleanup,
            int deactivateCount,
            bool hasEventKeyWhileActive,
            bool eventIgnoredAfterCleanup,
            bool buffTagWhileActive,
            bool buffTagAfterCleanup,
            int activeEffectCountAfterCleanup,
            bool activeHandleCreated)
        {
            ApplyCount = applyCount;
            RemoveCount = removeCount;
            EventCountWhileActive = eventCountWhileActive;
            EventCountAfterCleanup = eventCountAfterCleanup;
            DeactivateCount = deactivateCount;
            HasEventKeyWhileActive = hasEventKeyWhileActive;
            EventIgnoredAfterCleanup = eventIgnoredAfterCleanup;
            BuffTagWhileActive = buffTagWhileActive;
            BuffTagAfterCleanup = buffTagAfterCleanup;
            ActiveEffectCountAfterCleanup = activeEffectCountAfterCleanup;
            ActiveHandleCreated = activeHandleCreated;
        }

        public int ApplyCount { get; }
        public int RemoveCount { get; }
        public int EventCountWhileActive { get; }
        public int EventCountAfterCleanup { get; }
        public int DeactivateCount { get; }
        public bool HasEventKeyWhileActive { get; }
        public bool EventIgnoredAfterCleanup { get; }
        public bool BuffTagWhileActive { get; }
        public bool BuffTagAfterCleanup { get; }
        public int ActiveEffectCountAfterCleanup { get; }
        public bool ActiveHandleCreated { get; }
    }

    public sealed class PFGASSampleLifecycleCounters
    {
        public int ApplyCount;
        public int RemoveCount;
        public int EventCount;
        public int DeactivateCount;
    }

    internal sealed class PFGASSampleCountingExecution : IGameplayEffectExecution
    {
        private readonly PFGASSampleLifecycleCounters counters;
        private readonly GameplayEffectExecutionPhase phase;

        public PFGASSampleCountingExecution(
            PFGASSampleLifecycleCounters counters,
            GameplayEffectExecutionPhase phase)
        {
            this.counters = counters;
            this.phase = phase;
        }

        public GASResult Execute(GameplayEffectExecutionContext context)
        {
            if (phase == GameplayEffectExecutionPhase.OnApply)
            {
                counters.ApplyCount++;
            }
            else if (phase == GameplayEffectExecutionPhase.OnRemove)
            {
                counters.RemoveCount++;
            }

            return GASResult.Success();
        }
    }

    internal sealed class PFGASSampleEventTrigger : IGameplayEffectTrigger
    {
        private readonly string eventName;
        private readonly PFGASSampleLifecycleCounters counters;
        private Action<GameplayEvent> handler;

        public PFGASSampleEventTrigger(string eventName, PFGASSampleLifecycleCounters counters)
        {
            this.eventName = eventName;
            this.counters = counters;
        }

        public GASResult Activate(GameplayEffectTriggerContext context)
        {
            handler = gameplayEvent =>
            {
                if (gameplayEvent.Target == context.Target)
                {
                    counters.EventCount++;
                }
            };
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
}
