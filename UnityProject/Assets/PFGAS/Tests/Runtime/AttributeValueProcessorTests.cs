using System;
using NUnit.Framework;

namespace PFGAS.Runtime.Tests
{
    public sealed class AttributeValueProcessorTests
    {
        [Test]
        public void FullHeal_DoesNotRaiseHpBaseOrCurrentAboveMaxHp()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var unit = factory.CreateUnit("HealTarget", hp: 100f, maxHp: 100f);

                RequireSuccess(unit.Effects.ApplyToSelf(PFGASSampleEffects.CreateInstantHeal(20f), unit));

                Assert.That(unit.Attributes.GetBaseValue(PFAttributeId.HP), Is.EqualTo(100f));
                Assert.That(unit.Attributes.GetCurrentValue(PFAttributeId.HP), Is.EqualTo(100f));
            }
        }

        [Test]
        public void Damage_UsesVisibleHpAndDoesNotDropBelowZero()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var unit = factory.CreateUnit("DamageTarget", hp: 100f, maxHp: 100f);

                RequireSuccess(unit.Effects.ApplyToSelf(PFGASSampleEffects.CreateInstantDamage(25f), unit));

                Assert.That(unit.Attributes.GetBaseValue(PFAttributeId.HP), Is.EqualTo(75f));
                Assert.That(unit.Attributes.GetCurrentValue(PFAttributeId.HP), Is.EqualTo(75f));

                RequireSuccess(unit.Effects.ApplyToSelf(PFGASSampleEffects.CreateInstantDamage(100f), unit));

                Assert.That(unit.Attributes.GetBaseValue(PFAttributeId.HP), Is.EqualTo(0f));
                Assert.That(unit.Attributes.GetCurrentValue(PFAttributeId.HP), Is.EqualTo(0f));
            }
        }

        [Test]
        public void LoweringMaxHp_ReprocessesHpBaseValue()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var unit = factory.CreateUnit("MaxHpLowerTarget", hp: 100f, maxHp: 100f);

                unit.Attributes.SetBaseValue(PFAttributeId.MaxHP, 60f);

                Assert.That(unit.Attributes.GetBaseValue(PFAttributeId.HP), Is.EqualTo(60f));
                Assert.That(unit.Attributes.GetCurrentValue(PFAttributeId.HP), Is.EqualTo(60f));
            }
        }

        [Test]
        public void RaisingMaxHp_DoesNotHealHp()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var unit = factory.CreateUnit("MaxHpRaiseTarget", hp: 50f, maxHp: 100f);

                unit.Attributes.SetBaseValue(PFAttributeId.MaxHP, 150f);

                Assert.That(unit.Attributes.GetBaseValue(PFAttributeId.HP), Is.EqualTo(50f));
                Assert.That(unit.Attributes.GetCurrentValue(PFAttributeId.HP), Is.EqualTo(50f));
            }
        }

        [Test]
        public void BaseValueProcessorCycle_IsRejected()
        {
            var graph = new AttributeGraph();
            var rules = new[]
            {
                new AttributeRule(
                    PFAttributeId.HP,
                    1f,
                    baseValueProcessor: new ClampBaseValueProcessor(0f, PFAttributeId.MaxHP)),
                new AttributeRule(
                    PFAttributeId.MaxHP,
                    1f,
                    baseValueProcessor: new ClampBaseValueProcessor(0f, PFAttributeId.HP)),
            };

            Assert.Throws<InvalidOperationException>(() => graph.AddAttributes(rules));
        }

        [Test]
        public void PeriodicRegen_DoesNotStackOverMaxHpBase()
        {
            using (var factory = new PFGASSampleUnitFactory())
            {
                var unit = factory.CreateUnit("RegenTarget", hp: 100f, maxHp: 100f);

                RequireSuccess(unit.Effects.ApplyToSelf(PFGASSampleEffects.CreateRefreshRegen(), unit));
                unit.Effects.Tick(1f);
                unit.Effects.Tick(1f);

                Assert.That(unit.Attributes.GetBaseValue(PFAttributeId.HP), Is.EqualTo(100f));
                Assert.That(unit.Attributes.GetCurrentValue(PFAttributeId.HP), Is.EqualTo(100f));
            }
        }

        private static GameplayEffectApplyResult RequireSuccess(
            GASResult<GameplayEffectApplyResult> result)
        {
            Assert.That(result.Failed, Is.False, result.Failure.ToString());
            return result.Value;
        }
    }
}
