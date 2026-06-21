using System.Collections.Generic;
using NUnit.Framework;

namespace PFGAS.Runtime.Tests
{
    public sealed class AttributeMagnitudeTests
    {
        private const PFAttributeId Target = (PFAttributeId)100;
        private const PFAttributeId Dependency = (PFAttributeId)101;
        private const PFAttributeId FirstDependency = (PFAttributeId)102;
        private const PFAttributeId SecondDependency = (PFAttributeId)103;

        [Test]
        public void FixedMagnitude_EvaluatesValueAndHasNoDependencies()
        {
            var magnitude = AttributeMagnitude.Fixed(25f);

            Assert.That(magnitude.Evaluate(null), Is.EqualTo(25f));
            Assert.That(magnitude.Dependencies, Is.Empty);
        }

        [Test]
        public void FixedMagnitude_ModifierAddsToTarget()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(Target, new AttributeValue(10f));

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(Target, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
            }));

            Assert.That(graph.GetCurrentValue(Target), Is.EqualTo(15f));
        }

        [Test]
        public void ScalableFloatMagnitude_EvaluatesLinearValue()
        {
            var magnitude = AttributeMagnitude.ScalableFloat(10f, 3f, 0.5f);

            Assert.That(magnitude.Evaluate(null), Is.EqualTo(30.5f));
            Assert.That(magnitude.Dependencies, Is.Empty);
        }

        [Test]
        public void ScalableFloatMagnitude_ModifierAddsScaledValueToTarget()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(Target, new AttributeValue(10f));

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(Target, GEOperation.Add, AttributeMagnitude.ScalableFloat(3f, 2f, 1f)),
            }));

            Assert.That(graph.GetCurrentValue(Target), Is.EqualTo(17f));
        }

        [Test]
        public void AttributeBasedMagnitude_DeclaresDependencyAndRecalculatesTarget()
        {
            var magnitude = AttributeMagnitude.AttributeBased(Dependency, 0.5f, 2f);
            Assert.That(magnitude.Dependencies.Count, Is.EqualTo(1));
            Assert.That(magnitude.Dependencies[0], Is.EqualTo(Dependency));

            var graph = new AttributeGraph();
            graph.AddAttribute(Dependency, new AttributeValue(20f));
            graph.AddAttribute(Target, new AttributeValue(5f));

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(Target, GEOperation.Add, magnitude),
            }));

            Assert.That(graph.GetCurrentValue(Target), Is.EqualTo(17f));

            graph.SetBaseValue(Dependency, 40f);

            Assert.That(graph.GetCurrentValue(Target), Is.EqualTo(27f));
        }

        [Test]
        public void AttributeBasedMagnitude_DependencyParticipatesInCycleDetection()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(Target, new AttributeValue(10f));
            graph.AddAttribute(Dependency, new AttributeValue(20f));
            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(Target, GEOperation.Add, AttributeMagnitude.AttributeBased(Dependency)),
            }));

            Assert.Throws<System.InvalidOperationException>(() =>
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(Dependency, GEOperation.Add, AttributeMagnitude.AttributeBased(Target)),
                })));
        }

        [Test]
        public void CustomMagnitude_CanUseMultipleDependenciesAndRecalculateTarget()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(FirstDependency, new AttributeValue(20f));
            graph.AddAttribute(SecondDependency, new AttributeValue(3f));
            graph.AddAttribute(Target, new AttributeValue(0f));

            var magnitude = new TwoDependencyMagnitude(FirstDependency, SecondDependency);
            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(Target, GEOperation.Override, magnitude),
            }));

            Assert.That(magnitude.Dependencies.Count, Is.EqualTo(2));
            Assert.That(graph.GetCurrentValue(Target), Is.EqualTo(16f));

            graph.SetBaseValue(SecondDependency, 4f);

            Assert.That(graph.GetCurrentValue(Target), Is.EqualTo(18f));

            graph.SetBaseValue(FirstDependency, 30f);

            Assert.That(graph.GetCurrentValue(Target), Is.EqualTo(23f));
        }

        private sealed class TwoDependencyMagnitude : IAttributeMagnitude
        {
            private readonly PFAttributeId firstDependency;
            private readonly PFAttributeId secondDependency;
            private readonly PFAttributeId[] dependencies;

            public TwoDependencyMagnitude(PFAttributeId firstDependency, PFAttributeId secondDependency)
            {
                this.firstDependency = firstDependency;
                this.secondDependency = secondDependency;
                dependencies = new[] { firstDependency, secondDependency };
            }

            public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

            public float Evaluate(AttributeGraphContext context)
            {
                return context.GetCurrentValue(firstDependency) * 0.5f +
                       context.GetCurrentValue(secondDependency) * 2f;
            }
        }
    }
}
