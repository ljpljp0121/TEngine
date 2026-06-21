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

    internal static class PFGASTestTagIds
    {
        public static readonly PFTagId State = new PFTagId(0);
        public static readonly PFTagId State_Buff = new PFTagId(1);
        public static readonly PFTagId State_DeBuff = new PFTagId(2);
        public static readonly PFTagId Life = new PFTagId(3);
        public static readonly PFTagId Life_HP = new PFTagId(4);
        public static readonly PFTagId State_DeBuff_Du = new PFTagId(5);
        public static readonly PFTagId State_DeBuff_Fire = new PFTagId(6);
        public static readonly PFTagId State_DeBuff_Ice = new PFTagId(7);
        public static readonly PFTagId Life_MP = new PFTagId(8);
    }

    public static class PFGASSampleEffects
    {
        public const string LifecycleEventName = "PFGAS.Sample.Hit";

        public static GameplayEffect CreateInstantDamage(float amount = 25f)
        {
            return new GameplayEffect(
                "Sample.InstantDamage",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-Mathf.Abs(amount)),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });
        }

        public static GameplayEffect CreateInstantHeal(float amount = 20f)
        {
            return new GameplayEffect(
                "Sample.InstantHeal",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(Mathf.Abs(amount)),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });
        }

        public static GameplayEffect CreateBurningDot()
        {
            return new GameplayEffect(
                "Sample.BurningDot",
                GameplayEffectLifetime.ForDuration(3f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, -0.08f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                tags: new GameplayEffectTagRequirements(
                    blockedTargetTags: new[] { PFGASTestTagIds.State_DeBuff_Ice }),
                stacking: GameplayEffectStackingPolicy.Refresh(),
                grantedTags: new[] { PFGASTestTagIds.State_DeBuff_Fire });
        }

        public static GameplayEffect CreateScalingBurningDot()
        {
            return new GameplayEffect(
                "Sample.ScalingBurningDot",
                GameplayEffectLifetime.ForDuration(3f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, -0.06f),
                        GameplayEffectCapturePolicy.ReevaluateOnPeriod),
                },
                tags: new GameplayEffectTagRequirements(
                    blockedTargetTags: new[] { PFGASTestTagIds.State_DeBuff_Ice }),
                stacking: GameplayEffectStackingPolicy.Refresh(),
                grantedTags: new[] { PFGASTestTagIds.State_DeBuff_Fire });
        }

        public static GameplayEffect CreateStackingPoison()
        {
            return new GameplayEffect(
                "Sample.Poison",
                GameplayEffectLifetime.ForDuration(4f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, -0.04f),
                        GameplayEffectCapturePolicy.SnapshotOnApply,
                        scaleByStackCount: true),
                },
                stacking: GameplayEffectStackingPolicy.Stack(
                    5,
                    GameplayEffectStackingScope.ByTarget,
                    refreshDurationOnStack: true,
                    overflowPolicy: GameplayEffectOverflowPolicy.Refresh),
                grantedTags: new[] { PFGASTestTagIds.State_DeBuff_Du });
        }

        public static GameplayEffect CreateIndependentBleed()
        {
            return new GameplayEffect(
                "Sample.Bleed",
                GameplayEffectLifetime.ForDuration(4f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-3f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                stacking: GameplayEffectStackingPolicy.Independent(),
                grantedTags: new[] { PFGASTestTagIds.State_DeBuff });
        }

        public static GameplayEffect CreateSnapshotLeadershipAura()
        {
            return new GameplayEffect(
                "Sample.SnapshotLeadershipAura",
                GameplayEffectLifetime.Infinite(),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.2f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                stacking: GameplayEffectStackingPolicy.Refresh(),
                grantedTags: new[] { PFGASTestTagIds.State_Buff });
        }

        public static GameplayEffect CreateRiskyDynamicLeadershipAura()
        {
            return new GameplayEffect(
                "Sample.RiskyDynamicLeadershipAura",
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
                stacking: GameplayEffectStackingPolicy.Refresh(),
                grantedTags: new[] { PFGASTestTagIds.State_Buff });
        }

        public static GameplayEffect CreateTargetLocalShield()
        {
            return new GameplayEffect(
                "Sample.TargetLocalShield",
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
                stacking: GameplayEffectStackingPolicy.Replace(),
                grantedTags: new[] { PFGASTestTagIds.State_Buff });
        }

        public static GameplayEffect CreateRefreshRegen()
        {
            return new GameplayEffect(
                "Sample.RefreshRegen",
                GameplayEffectLifetime.ForDuration(5f, period: 1f, executePeriodicOnApply: true),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(6f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                stacking: GameplayEffectStackingPolicy.Refresh(),
                grantedTags: new[] { PFGASTestTagIds.State_Buff });
        }

        public static GameplayEffect CreateReplaceFortify(float maxHpBonus)
        {
            return new GameplayEffect(
                "Sample.ReplaceFortify",
                GameplayEffectLifetime.ForDuration(8f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(maxHpBonus),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                },
                stacking: GameplayEffectStackingPolicy.Replace(),
                grantedTags: new[] { PFGASTestTagIds.State_Buff });
        }

        public static GameplayEffect CreateLifecycleEvent(
            PFGASSampleLifecycleCounters counters,
            string eventName = LifecycleEventName)
        {
            return CreateLifecycleEvent(
                counters,
                eventName,
                GameplayEffectLifetime.ForDuration(2f));
        }

        public static GameplayEffect CreatePersistentLifecycleEvent(
            PFGASSampleLifecycleCounters counters,
            string eventName = LifecycleEventName)
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
                "Sample.LifecycleEvent",
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
        public const string LifecycleEventName = PFGASSampleEffects.LifecycleEventName;

        public static IReadOnlyList<string> RunAllSampleSummaries()
        {
            return new[]
            {
                RunInstantDamageAndHeal(),
                RunBurningDot(),
                RunScalingBurningDot(),
                RunStackingPoison(),
                RunIndependentBleed(),
                RunSnapshotLeadershipAura(),
                RunTargetLocalShield(),
                RunRefreshRegen(),
                RunReplaceFortify(),
                RunLifecycleEvent(),
                RunRiskyDynamicAuraOneWay(),
            };
        }

        public static string RunInstantDamageAndHeal()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleInstantSource");
                var target = factory.CreateUnit("SampleInstantTarget");

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateInstantDamage(25f), source));
                var afterDamage = target.Attributes.GetBaseValue(PFAttributeId.HP);
                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateInstantHeal(10f), source));

                return "瞬时伤害/治疗：HP 100 -> " + Format(afterDamage) + " -> " +
                       Format(target.Attributes.GetBaseValue(PFAttributeId.HP)) +
                       "，激活效果=" + target.Effects.ActiveEffectCount;
            }
        }

        public static string RunBurningDot()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleBurningSource", maxHp: 100f);
                var target = factory.CreateUnit("SampleBurningTarget");
                var iceTarget = factory.CreateUnit("SampleBurningIceTarget");

                iceTarget.Tags.AddLooseTag(PFGASTestTagIds.State_DeBuff_Ice);
                var blocked = iceTarget.Effects.ApplyToSelf(PFGASSampleEffects.CreateBurningDot(), source);

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateBurningDot(), source));
                target.Effects.Tick(1f);
                var firstTick = target.Attributes.GetBaseValue(PFAttributeId.HP);
                source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
                target.Effects.Tick(1f);
                var secondTick = target.Attributes.GetBaseValue(PFAttributeId.HP);
                target.Effects.Tick(1f);

                return "快照灼烧 DoT：冰冻阻挡=" + blocked.Failed +
                       "，周期后 HP=" + Format(firstTick) + "/" + Format(secondTick) +
                       "，已过期=" + (target.Effects.ActiveEffectCount == 0);
            }
        }

        public static string RunScalingBurningDot()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleScalingBurningSource", maxHp: 100f);
                var target = factory.CreateUnit("SampleScalingBurningTarget");

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateScalingBurningDot(), source));
                target.Effects.Tick(1f);
                var firstTick = target.Attributes.GetBaseValue(PFAttributeId.HP);
                source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
                target.Effects.Tick(1f);
                var secondTick = target.Attributes.GetBaseValue(PFAttributeId.HP);

                return "动态周期 DoT：来源 MaxHP 改变后，周期 HP=" +
                       Format(firstTick) + "/" + Format(secondTick);
            }
        }

        public static string RunStackingPoison()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var sourceA = factory.CreateUnit("SamplePoisonSourceA", maxHp: 100f);
                var sourceB = factory.CreateUnit("SamplePoisonSourceB", maxHp: 200f);
                var target = factory.CreateUnit("SamplePoisonTarget");
                var poison = PFGASSampleEffects.CreateStackingPoison();

                var firstApply = RequireSuccess(target.Effects.ApplyToSelf(poison, sourceA));
                RequireSuccess(target.Effects.ApplyToSelf(poison, sourceB));
                RequireSuccess(target.Effects.ApplyToSelf(poison, sourceA));
                target.Effects.TryGetActiveEffect(firstApply.Handle, out var active);

                target.Effects.Tick(1f);
                var afterTick = target.Attributes.GetBaseValue(PFAttributeId.HP);

                return "中毒按 EffectId/目标叠层：激活效果=" + target.Effects.ActiveEffectCount +
                       "，层数=" + (active != null ? active.StackCount : 0) +
                       "，HP=" + Format(afterTick);
            }
        }

        public static string RunIndependentBleed()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleBleedSource");
                var target = factory.CreateUnit("SampleBleedTarget");

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateIndependentBleed(), source));
                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateIndependentBleed(), source));
                target.Effects.Tick(1f);

                return "独立流血：激活效果=" + target.Effects.ActiveEffectCount +
                       "，HP=" + Format(target.Attributes.GetBaseValue(PFAttributeId.HP));
            }
        }

        public static string RunSnapshotLeadershipAura()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleAuraSource", maxHp: 100f);
                var target = factory.CreateUnit("SampleAuraTarget");

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateSnapshotLeadershipAura(), source));
                var beforeSourceChange = target.Attributes.GetCurrentValue(PFAttributeId.MaxHP);
                source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 300f);
                target.Effects.Tick(0f);

                return "快照光环：目标 MaxHP 保持 " +
                       Format(beforeSourceChange) + " -> " +
                       Format(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP));
            }
        }

        public static string RunTargetLocalShield()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleShieldSource");
                var target = factory.CreateUnit("SampleShieldTarget", hp: 0f, maxHp: 100f);

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateTargetLocalShield(), source));
                var afterApply = target.Attributes.GetCurrentValue(PFAttributeId.HP);
                target.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
                var afterMaxHpChange = target.Attributes.GetCurrentValue(PFAttributeId.HP);

                return "目标本地护盾：HP 当前值 " +
                       Format(afterApply) + " -> " + Format(afterMaxHpChange);
            }
        }

        public static string RunRefreshRegen()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleRegenSource");
                var target = factory.CreateUnit("SampleRegenTarget", hp: 40f, maxHp: 100f);
                var regen = PFGASSampleEffects.CreateRefreshRegen();

                var firstApply = RequireSuccess(target.Effects.ApplyToSelf(regen, source));
                target.Effects.Tick(2f);
                var hpBeforeRefresh = target.Attributes.GetBaseValue(PFAttributeId.HP);
                RequireSuccess(target.Effects.ApplyToSelf(regen, source));
                target.Effects.TryGetActiveEffect(firstApply.Handle, out var active);

                var fullTarget = factory.CreateUnit("SampleFullRegenTarget", hp: 100f, maxHp: 100f);
                RequireSuccess(fullTarget.Effects.ApplyToSelf(regen, source));
                fullTarget.Effects.Tick(2f);

                return "刷新型再生：HP=" + Format(hpBeforeRefresh) +
                       "，激活效果=" + target.Effects.ActiveEffectCount +
                       "，刷新后剩余时间=" + Format(active != null ? active.RemainingTime : 0f) +
                       "，满血 HP Base/Current=" +
                       Format(fullTarget.Attributes.GetBaseValue(PFAttributeId.HP)) +
                       "/" +
                       Format(fullTarget.Attributes.GetCurrentValue(PFAttributeId.HP));
            }
        }

        public static string RunReplaceFortify()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleFortifySource");
                var target = factory.CreateUnit("SampleFortifyTarget");

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateReplaceFortify(15f), source));
                var weakMaxHp = target.Attributes.GetCurrentValue(PFAttributeId.MaxHP);
                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateReplaceFortify(40f), source));

                return "替换型强化：MaxHP " + Format(weakMaxHp) +
                       " -> " + Format(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP)) +
                       "，激活效果=" + target.Effects.ActiveEffectCount;
            }
        }

        public static string RunLifecycleEvent()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleLifecycleSource");
                var target = factory.CreateUnit("SampleLifecycleTarget");
                var counters = new PFGASSampleLifecycleCounters();

                RequireSuccess(target.Effects.ApplyToSelf(
                    PFGASSampleEffects.CreateLifecycleEvent(counters),
                    source));
                target.GameplayEventBus.Publish(LifecycleEventName, source, target);
                target.Effects.Tick(2f);
                target.GameplayEventBus.Publish(LifecycleEventName, source, target);

                return "生命周期：应用/移除/事件/停用=" +
                       counters.ApplyCount + "/" + counters.RemoveCount + "/" +
                       counters.EventCount + "/" + counters.DeactivateCount;
            }
        }

        public static string RunRiskyDynamicAuraOneWay()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var source = factory.CreateUnit("SampleRiskyAuraSource", maxHp: 100f);
                var target = factory.CreateUnit("SampleRiskyAuraTarget");

                RequireSuccess(target.Effects.ApplyToSelf(PFGASSampleEffects.CreateRiskyDynamicLeadershipAura(), source));
                var first = target.Attributes.GetCurrentValue(PFAttributeId.MaxHP);
                source.Attributes.SetBaseValue(PFAttributeId.MaxHP, 200f);
                target.Effects.Tick(0f);

                return "危险动态光环单向示例：MaxHP " +
                       Format(first) + " -> " +
                       Format(target.Attributes.GetCurrentValue(PFAttributeId.MaxHP)) +
                       "（不要配置成双向来源循环）";
            }
        }

        private static GameplayEffectApplyResult RequireSuccess(
            GASResult<GameplayEffectApplyResult> result)
        {
            if (result.Failed)
            {
                throw new InvalidOperationException(result.Failure.ToString());
            }

            return result.Value;
        }

        private static string Format(float value)
        {
            return value.ToString("0.##");
        }
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

