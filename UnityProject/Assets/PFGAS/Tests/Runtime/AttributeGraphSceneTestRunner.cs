using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFGAS.Runtime.Tests
{
    public sealed class AttributeGraphSceneTestRunner : MonoBehaviour
    {
        private const PFAttributeId A = (PFAttributeId)9100000;
        private const PFAttributeId B = (PFAttributeId)9100001;
        private const PFAttributeId C = (PFAttributeId)9100002;
        private const PFAttributeId D = (PFAttributeId)9100003;

        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool stopOnFirstFailure;
        [SerializeField] private bool drawRuntimePanel = true;
        [SerializeField] private int passedCount;
        [SerializeField] private int failedCount;
        [SerializeField] private List<string> lastResults = new List<string>();

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

        [ContextMenu("Run AttributeGraph Scene Tests")]
        public void RunAll()
        {
            passedCount = 0;
            failedCount = 0;
            lastResults.Clear();

            RunCase("AttributeValue clamps base and current", AttributeValueClampsBaseAndCurrentValues);
            RunCase("AttributeRule registration", AttributeRulesRegisterRequiredAttributesAndGraphNodes);
            RunCase("AttributeRule dependency snapshot", AttributeRuleUsesDependencySnapshot);
            RunCase("ClampMax evaluator uses dependency current", ClampMaxEvaluatorClampsTargetByDependencyCurrentValue);
            RunCase("Clamp evaluator variants use dependencies", ClampEvaluatorVariantsUseDependencyCurrentValues);
            RunCase("Formula evaluator uses dependencies and raw modifiers", FormulaEvaluatorUsesDependenciesAndRawValueModifiers);
            RunCase("SetBaseValue only refreshes dependents", SetBaseValueRecalculatesOnlyChangedAttributeAndDependents);
            RunCase("SetBaseValue rollback on evaluator failure", SetBaseValueRollsBackWhenEvaluatorFails);
            RunCase("SetEvaluator rollback restores dependencies", SetEvaluatorRollbackRestoresDependencies);
            RunCase("Independent leaf does not refresh unrelated nodes", IndependentLeafDoesNotRecalculateUnrelatedNodes);
            RunCase("ModifierSource only refreshes target dependents", ModifierSourceRecalculatesOnlyTargetsAndDependents);
            RunCase("AddModifierSource rollback on magnitude failure", AddModifierSourceRollbackOnMagnitudeFailure);
            RunCase("RemoveModifierSource rollback on recalculation failure", RemoveModifierSourceRollbackOnRecalculationFailure);
            RunCase("RemoveModifierSource rollback preserves modifier order", RemoveModifierSourceRollbackPreservesModifierOrder);
            RunCase("Stacking aggregation order", StackingAggregationAddsThenMultipliesAndOverrideWins);
            RunCase("Min/Max aggregation mode", MinAndMaxAggregationSelectBestSingleModifierResult);
            RunCase("Composed magnitude dependencies", ComposedMagnitudeUsesAttributeDependencies);
            RunCase("Magnitude operation tree", MagnitudeOperationsComposeValuesAndDependencies);
            RunCase("Magnitude validation", MagnitudeValidationRejectsInvalidCalculations);
            RunCase("Magnitude source removal cleans dependencies", DynamicMagnitudeDependencyRemovalCleansEdges);
            RunCase("Magnitude dependency cycle rolls back", MagnitudeDependencyCycleRollsBackModifierSource);
            RunCase("Magnitude missing dependencies fail fast", MagnitudeMissingDependenciesFailFastWithoutMutatingGraph);
            RunCase("Cycle registration rolls back", CycleRegistrationFailsAndRestoresPreviousGraph);
            RunCase("Batch cycle registration fails immediately", BatchCycleRegistrationFailsBeforeEndBatch);
            RunCase("Batch update refreshes once", BatchUpdateRecalculatesOnceAtEndUsingFinalValues);
            RunCase("Attribute change events are batched", AttributeChangeEventsAreBatchedAfterRecalculate);
            RunCase("Non-finite values are rejected", NonFiniteValuesAreRejected);
            RunCase("RemoveAttribute rejects live dependencies", RemoveAttributeRejectsLiveDependencies);
            RunCase("RemoveAttribute rollback on recalculation failure", RemoveAttributeRollbackOnRecalculationFailure);
            RunCase("Attribute registration rollback uses transaction", AttributeRegistrationRollbackUsesTransaction);
            RunCase("Invalid registrations do not mutate state", InvalidRegistrationsFailFastWithoutMutatingUsableState);

            if (failedCount == 0)
            {
                Debug.Log($"PFGAS AttributeGraph scene tests passed: {passedCount} cases.", this);
            }
            else
            {
                Debug.LogError(
                    $"PFGAS AttributeGraph scene tests failed: {failedCount} failed, {passedCount} passed.",
                    this);
            }
        }

        private void OnGUI()
        {
            if (!drawRuntimePanel || lastResults.Count == 0)
            {
                return;
            }

            var height = Mathf.Min(500f, 44f + lastResults.Count * 22f);
            GUILayout.BeginArea(new Rect(12f, 12f, 820f, height), GUI.skin.box);
            GUILayout.Label($"PFGAS AttributeGraph Tests  Passed: {passedCount}  Failed: {failedCount}");
            for (var i = 0; i < lastResults.Count; i++)
            {
                GUILayout.Label(lastResults[i]);
            }

            GUILayout.EndArea();
        }

        private void RunCase(string caseName, Action action)
        {
            try
            {
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
        }

        private static void AttributeValueClampsBaseAndCurrentValues()
        {
            var value = new AttributeValue(20f, minValue: 0f, maxValue: 10f);

            ExpectApproximately(10f, value.BaseValue, "Initial BaseValue");
            ExpectApproximately(10f, value.CurrentValue, "Initial CurrentValue");

            value.SetBaseValue(-5f);
            value.SetCurrentValue(99f);

            ExpectApproximately(0f, value.BaseValue, "Clamped BaseValue");
            ExpectApproximately(10f, value.CurrentValue, "Clamped CurrentValue");
        }

        private static void AttributeRulesRegisterRequiredAttributesAndGraphNodes()
        {
            var maxHp = new AttributeRule(
                A,
                100f,
                minValue: 1f,
                evaluator: DefaultAttributeEvaluator.Instance);
            var hp = new AttributeRule(
                B,
                100f,
                minValue: 0f,
                evaluator: new ClampMaxAttributeEvaluator(A));

            ExpectEqual(1, hp.RequiredAttributes.Count, "HP required attribute count");
            ExpectEqual((int)A, (int)hp.RequiredAttributes[0], "HP required attribute id");

            var graph = new AttributeGraph();
            graph.AddAttributes(new[] { hp, maxHp });

            ExpectApproximately(100f, graph.GetCurrentValue(B), "Initial HP");
            graph.SetBaseValue(A, 80f);
            ExpectApproximately(80f, graph.GetCurrentValue(B), "HP clamped by MaxHP after mutation");

            ExpectThrows<InvalidOperationException>(() =>
                new AttributeGraph().AddAttributes(new[] { hp }), "Rule missing dependency");

            var cycleA = new AttributeRule(
                A,
                1f,
                evaluator: new ClampMaxAttributeEvaluator(B));
            var cycleB = new AttributeRule(
                B,
                1f,
                evaluator: new ClampMaxAttributeEvaluator(A));
            ExpectThrows<InvalidOperationException>(() =>
                new AttributeGraph().AddAttributes(new[] { cycleA, cycleB }), "Rule dependency cycle");

            var clampedRule = new AttributeRule(C, 2f, minValue: 3f);
            var clampedGraph = new AttributeGraph();
            clampedGraph.AddAttribute(clampedRule);
            ExpectApproximately(3f, clampedGraph.GetCurrentValue(C), "Rule default clamps through AttributeValue");
        }

        private static void AttributeRuleUsesDependencySnapshot()
        {
            var evaluator = new MutableDependencyEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A));
            var rule = new AttributeRule(B, 2f, evaluator: evaluator);
            evaluator.MutableDependencies.Clear();
            evaluator.MutableDependencies.Add(C);

            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttributes(new[] { rule });

            ExpectApproximately(12f, graph.GetCurrentValue(B), "Initial value from rule dependency snapshot");
            graph.SetBaseValue(A, 20f);
            ExpectApproximately(22f, graph.GetCurrentValue(B), "Mutated dependency list did not replace snapshot edge");
        }

        private static void ClampMaxEvaluatorClampsTargetByDependencyCurrentValue()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(100f, minValue: 0f));
            graph.AddAttribute(B, new AttributeValue(200f, minValue: 0f), new ClampMaxAttributeEvaluator(A));

            ExpectApproximately(100f, graph.GetCurrentValue(B), "Initial clamped B");

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(50f)),
            }));

            ExpectApproximately(150f, graph.GetCurrentValue(A), "Modified A");
            ExpectApproximately(150f, graph.GetCurrentValue(B), "B clamped by modified A");
        }

        private static void ClampEvaluatorVariantsUseDependencyCurrentValues()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(B, new AttributeValue(100f));
            graph.AddAttribute(C, new AttributeValue(5f), new ClampMinAttributeEvaluator(A));
            graph.AddAttribute(D, new AttributeValue(50f), new ClampRangeAttributeEvaluator(A, B));

            ExpectApproximately(10f, graph.GetCurrentValue(C), "Initial ClampMin C");
            ExpectApproximately(50f, graph.GetCurrentValue(D), "Initial ClampRange D");

            graph.SetBaseValue(A, 20f);
            graph.SetBaseValue(B, 30f);

            ExpectApproximately(20f, graph.GetCurrentValue(C), "ClampMin C after min mutation");
            ExpectApproximately(30f, graph.GetCurrentValue(D), "ClampRange D after max mutation");
        }

        private static void FormulaEvaluatorUsesDependenciesAndRawValueModifiers()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(B, new AttributeValue(5f));
            graph.AddAttribute(
                C,
                new AttributeValue(0f),
                new FormulaAttributeEvaluator(
                    new[] { A, B },
                    (context, _, rawValue) =>
                        context.GetCurrentValue(A) * 2f +
                        context.GetCurrentValue(B) +
                        rawValue));

            ExpectApproximately(25f, graph.GetCurrentValue(C), "Initial formula C");

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(C, GEOperation.Add, AttributeMagnitude.Fixed(20f)),
            }));
            graph.SetBaseValue(A, 12f);

            ExpectApproximately(49f, graph.GetCurrentValue(C), "Formula C after modifier and dependency change");
        }

        private static void SetBaseValueRecalculatesOnlyChangedAttributeAndDependents()
        {
            var graph = new AttributeGraph();
            var evalA = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);
            var evalB = new CountingEvaluator(new[] { A }, (context, _, raw) => raw + context.GetCurrentValue(A));
            var evalC = new CountingEvaluator(new[] { B }, (context, _, raw) => raw + context.GetCurrentValue(B));
            var evalD = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);

            graph.AddAttribute(A, new AttributeValue(1f), evalA);
            graph.AddAttribute(B, new AttributeValue(10f), evalB);
            graph.AddAttribute(C, new AttributeValue(100f), evalC);
            graph.AddAttribute(D, new AttributeValue(1000f), evalD);
            Reset(evalA, evalB, evalC, evalD);

            graph.SetBaseValue(A, 2f);

            ExpectEqual(1, evalA.Count, "A eval count");
            ExpectEqual(1, evalB.Count, "B eval count");
            ExpectEqual(1, evalC.Count, "C eval count");
            ExpectEqual(0, evalD.Count, "D eval count");
            ExpectApproximately(112f, graph.GetCurrentValue(C), "C current");
        }

        private static void SetBaseValueRollsBackWhenEvaluatorFails()
        {
            var graph = new AttributeGraph();
            var batchEvents = 0;
            var singleEvents = 0;

            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) =>
                {
                    if (context.GetCurrentValue(A) > 1f)
                    {
                        throw new InvalidOperationException("Intentional evaluator failure.");
                    }

                    return raw + context.GetCurrentValue(A);
                }));

            graph.AttributesChanged += _ => batchEvents++;
            graph.AttributeChanged += _ => singleEvents++;

            ExpectThrows<InvalidOperationException>(() =>
                graph.SetBaseValue(A, 5f), "Failing SetBaseValue evaluator");

            ExpectApproximately(1f, graph.GetBaseValue(A), "A base after failed SetBaseValue");
            ExpectApproximately(1f, graph.GetCurrentValue(A), "A current after failed SetBaseValue");
            ExpectApproximately(3f, graph.GetCurrentValue(B), "B current after failed SetBaseValue");
            ExpectEqual(0, batchEvents, "Batch events after failed SetBaseValue");
            ExpectEqual(0, singleEvents, "Single events after failed SetBaseValue");
        }

        private static void SetEvaluatorRollbackRestoresDependencies()
        {
            var graph = new AttributeGraph();
            var batchEvents = 0;
            var singleEvents = 0;
            var oldEval = new CountingEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A));

            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), oldEval);
            graph.AddAttribute(C, new AttributeValue(10f));

            graph.AttributesChanged += _ => batchEvents++;
            graph.AttributeChanged += _ => singleEvents++;

            ExpectThrows<InvalidOperationException>(() =>
                graph.SetEvaluator(B, new FormulaAttributeEvaluator(
                    new[] { C },
                    (_, __, ___) => throw new InvalidOperationException("Intentional evaluator failure."))),
                "Failing SetEvaluator");

            ExpectApproximately(3f, graph.GetCurrentValue(B), "B current after failed SetEvaluator");
            ExpectEqual(0, batchEvents, "Batch events after failed SetEvaluator");
            ExpectEqual(0, singleEvents, "Single events after failed SetEvaluator");

            Reset(oldEval);
            graph.SetBaseValue(C, 20f);
            ExpectEqual(0, oldEval.Count, "Old evaluator should not be dirtied by rolled-back dependency");

            graph.SetBaseValue(A, 2f);
            ExpectEqual(1, oldEval.Count, "Old evaluator should keep original dependency");
            ExpectApproximately(4f, graph.GetCurrentValue(B), "B current after dependency rollback");
        }

        private static void IndependentLeafDoesNotRecalculateUnrelatedNodes()
        {
            var graph = new AttributeGraph();
            var evalA = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);
            var evalB = new CountingEvaluator(new[] { A }, (context, _, raw) => raw + context.GetCurrentValue(A));
            var evalD = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);

            graph.AddAttribute(A, new AttributeValue(1f), evalA);
            graph.AddAttribute(B, new AttributeValue(10f), evalB);
            graph.AddAttribute(D, new AttributeValue(1000f), evalD);
            Reset(evalA, evalB, evalD);

            graph.SetBaseValue(D, 2000f);

            ExpectEqual(0, evalA.Count, "A eval count");
            ExpectEqual(0, evalB.Count, "B eval count");
            ExpectEqual(1, evalD.Count, "D eval count");
        }

        private static void ModifierSourceRecalculatesOnlyTargetsAndDependents()
        {
            var graph = new AttributeGraph();
            var evalA = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);
            var evalB = new CountingEvaluator(new[] { A }, (context, _, raw) => raw + context.GetCurrentValue(A));
            var evalC = new CountingEvaluator(new[] { B }, (context, _, raw) => raw + context.GetCurrentValue(B));
            var evalD = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);

            graph.AddAttribute(A, new AttributeValue(1f), evalA);
            graph.AddAttribute(B, new AttributeValue(10f), evalB);
            graph.AddAttribute(C, new AttributeValue(100f), evalC);
            graph.AddAttribute(D, new AttributeValue(1000f), evalD);
            Reset(evalA, evalB, evalC, evalD);

            var handle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(B, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
            }));

            ExpectEqual(0, evalA.Count, "A add eval count");
            ExpectEqual(1, evalB.Count, "B add eval count");
            ExpectEqual(1, evalC.Count, "C add eval count");
            ExpectEqual(0, evalD.Count, "D add eval count");
            ExpectApproximately(116f, graph.GetCurrentValue(C), "C current after add");

            Reset(evalA, evalB, evalC, evalD);
            Expect(graph.RemoveModifierSource(handle), "RemoveModifierSource returned false.");

            ExpectEqual(0, evalA.Count, "A remove eval count");
            ExpectEqual(1, evalB.Count, "B remove eval count");
            ExpectEqual(1, evalC.Count, "C remove eval count");
            ExpectEqual(0, evalD.Count, "D remove eval count");
            ExpectApproximately(111f, graph.GetCurrentValue(C), "C current after remove");
        }

        private static void AddModifierSourceRollbackOnMagnitudeFailure()
        {
            var graph = new AttributeGraph();
            var batchEvents = 0;
            var singleEvents = 0;
            var evalC = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);

            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(C, new AttributeValue(0f), evalC);

            graph.AttributesChanged += _ => batchEvents++;
            graph.AttributeChanged += _ => singleEvents++;

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(C, GEOperation.Add, new ThrowingMagnitude(new[] { A })),
                })), "Failing modifier magnitude");

            ExpectApproximately(0f, graph.GetCurrentValue(C), "C current after failed AddModifierSource");
            ExpectEqual(0, batchEvents, "Batch events after failed AddModifierSource");
            ExpectEqual(0, singleEvents, "Single events after failed AddModifierSource");

            Reset(evalC);
            graph.SetBaseValue(A, 20f);
            ExpectEqual(0, evalC.Count, "Rolled-back magnitude dependency should not dirty target");

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(C, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
            }));
            ExpectApproximately(5f, graph.GetCurrentValue(C), "C current after valid modifier");
        }

        private static void RemoveModifierSourceRollbackOnRecalculationFailure()
        {
            var graph = new AttributeGraph();
            var batchEvents = 0;
            var singleEvents = 0;

            graph.AddAttribute(A, new AttributeValue(0f));
            var handle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
            }));
            graph.SetEvaluator(A, new FormulaAttributeEvaluator(
                Array.Empty<PFAttributeId>(),
                (_, __, raw) =>
                {
                    if (raw < 5f)
                    {
                        throw new InvalidOperationException("Intentional evaluator failure.");
                    }

                    return raw;
                }));

            graph.AttributesChanged += _ => batchEvents++;
            graph.AttributeChanged += _ => singleEvents++;

            ExpectThrows<InvalidOperationException>(() =>
                graph.RemoveModifierSource(handle), "Failing RemoveModifierSource recalculation");

            ExpectApproximately(5f, graph.GetCurrentValue(A), "A current after failed RemoveModifierSource");
            ExpectEqual(0, batchEvents, "Batch events after failed RemoveModifierSource");
            ExpectEqual(0, singleEvents, "Single events after failed RemoveModifierSource");

            Expect(graph.SetBaseValue(A, 1f), "SetBaseValue should succeed if removed source was restored.");
            ExpectApproximately(6f, graph.GetCurrentValue(A), "A current after restored source mutation");

            graph.SetEvaluator(A, DefaultAttributeEvaluator.Instance);
            Expect(graph.RemoveModifierSource(handle), "Restored modifier source handle should remain removable.");
            ExpectApproximately(1f, graph.GetCurrentValue(A), "A current after removing restored source");
        }

        private static void RemoveModifierSourceRollbackPreservesModifierOrder()
        {
            var graph = new AttributeGraph();
            var shouldThrow = false;

            graph.AddAttribute(A, new AttributeValue(0f));
            var firstHandle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Override, AttributeMagnitude.Fixed(10f)),
            }));
            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Override, AttributeMagnitude.Fixed(20f)),
            }));
            graph.SetEvaluator(A, new FormulaAttributeEvaluator(
                Array.Empty<PFAttributeId>(),
                (_, __, raw) =>
                {
                    if (shouldThrow)
                    {
                        throw new InvalidOperationException("Intentional evaluator failure.");
                    }

                    return raw;
                }));

            ExpectApproximately(20f, graph.GetCurrentValue(A), "A current before failed remove");

            shouldThrow = true;
            ExpectThrows<InvalidOperationException>(() =>
                graph.RemoveModifierSource(firstHandle), "Failing RemoveModifierSource order rollback");
            ExpectApproximately(20f, graph.GetCurrentValue(A), "A current immediately after order rollback");

            shouldThrow = false;
            graph.RecalculateAll();
            ExpectApproximately(20f, graph.GetCurrentValue(A), "A current after order-preserving rollback");
        }

        private static void StackingAggregationAddsThenMultipliesAndOverrideWins()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(100f));

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(10f)),
                new AttributeModifier(A, GEOperation.Multiply, AttributeMagnitude.Fixed(2f)),
            }));

            ExpectApproximately(220f, graph.GetCurrentValue(A), "Stacking value");

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Override, AttributeMagnitude.Fixed(50f)),
            }));

            ExpectApproximately(50f, graph.GetCurrentValue(A), "Override value");
        }

        private static void MinAndMaxAggregationSelectBestSingleModifierResult()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(100f, AggregationMode.MinValueOnly));
            graph.AddAttribute(B, new AttributeValue(100f, AggregationMode.MaxValueOnly));

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(-10f)),
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(-30f)),
                new AttributeModifier(B, GEOperation.Add, AttributeMagnitude.Fixed(10f)),
                new AttributeModifier(B, GEOperation.Add, AttributeMagnitude.Fixed(30f)),
            }));

            ExpectApproximately(70f, graph.GetCurrentValue(A), "MinValueOnly current");
            ExpectApproximately(130f, graph.GetCurrentValue(B), "MaxValueOnly current");
        }

        private static void ComposedMagnitudeUsesAttributeDependencies()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(B, new AttributeValue(5f));
            graph.AddAttribute(C, new AttributeValue(0f));

            var magnitude = AttributeMagnitude.Add(
                AttributeMagnitude.Multiply(
                    AttributeMagnitude.Attribute(A),
                    AttributeMagnitude.Fixed(2f)),
                AttributeMagnitude.Attribute(B));

            ExpectEqual(2, magnitude.Dependencies.Count, "Composed magnitude dependency count");

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(C, GEOperation.Add, magnitude),
            }));

            ExpectApproximately(25f, graph.GetCurrentValue(C), "Initial composed magnitude");

            graph.SetBaseValue(A, 12f);

            ExpectApproximately(29f, graph.GetCurrentValue(C), "Composed magnitude after dependency change");
        }

        private static void MagnitudeOperationsComposeValuesAndDependencies()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(40f));
            graph.AddAttribute(B, new AttributeValue(5f));
            graph.AddAttribute(C, new AttributeValue(0f));

            var repeatedDependency = AttributeMagnitude.Add(
                AttributeMagnitude.Attribute(A),
                AttributeMagnitude.Multiply(
                    AttributeMagnitude.Attribute(A),
                    AttributeMagnitude.Fixed(0.5f)));
            ExpectEqual(1, repeatedDependency.Dependencies.Count, "Repeated dependency count");

            var magnitude = AttributeMagnitude.Clamp(
                AttributeMagnitude.Max(
                    AttributeMagnitude.Min(
                        AttributeMagnitude.Subtract(
                            AttributeMagnitude.Divide(
                                AttributeMagnitude.Attribute(A),
                                AttributeMagnitude.Attribute(B)),
                            AttributeMagnitude.Fixed(2f)),
                        AttributeMagnitude.Fixed(10f)),
                    AttributeMagnitude.Fixed(3f)),
                AttributeMagnitude.Fixed(0f),
                AttributeMagnitude.Fixed(6f));

            graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(C, GEOperation.Add, magnitude),
            }));

            ExpectEqual(2, magnitude.Dependencies.Count, "Operation tree dependency count");
            ExpectApproximately(6f, graph.GetCurrentValue(C), "Clamped operation tree value");

            graph.SetBaseValue(A, 20f);

            ExpectApproximately(3f, graph.GetCurrentValue(C), "Operation tree value after dependency change");
        }

        private static void MagnitudeValidationRejectsInvalidCalculations()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(B, new AttributeValue(0f));
            graph.AddAttribute(C, new AttributeValue(0f));

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(
                        C,
                        GEOperation.Add,
                        AttributeMagnitude.Divide(
                            AttributeMagnitude.Attribute(A),
                            AttributeMagnitude.Attribute(B))),
                })), "Divide by zero magnitude");

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(
                        C,
                        GEOperation.Add,
                        AttributeMagnitude.Clamp(
                            AttributeMagnitude.Attribute(A),
                            AttributeMagnitude.Fixed(20f),
                            AttributeMagnitude.Fixed(10f))),
                })), "Invalid clamp magnitude");

            ExpectApproximately(0f, graph.GetCurrentValue(C), "Target after invalid magnitudes");

            var evalC = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);
            var rollbackGraph = new AttributeGraph();
            rollbackGraph.AddAttribute(A, new AttributeValue(10f));
            rollbackGraph.AddAttribute(B, new AttributeValue(0f));
            rollbackGraph.AddAttribute(C, new AttributeValue(0f), evalC);

            ExpectThrows<InvalidOperationException>(() =>
                rollbackGraph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(
                        C,
                        GEOperation.Add,
                        AttributeMagnitude.Divide(
                            AttributeMagnitude.Attribute(A),
                            AttributeMagnitude.Attribute(B))),
                })), "Invalid magnitude rollback");

            Reset(evalC);
            rollbackGraph.SetBaseValue(A, 11f);

            ExpectEqual(0, evalC.Count, "Invalid magnitude must not leave dependency edges");
            ExpectApproximately(0f, rollbackGraph.GetCurrentValue(C), "Invalid magnitude must not leave modifiers");
        }

        private static void DynamicMagnitudeDependencyRemovalCleansEdges()
        {
            var graph = new AttributeGraph();
            var evalC = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);

            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(C, new AttributeValue(0f), evalC);

            var handle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(C, GEOperation.Add, AttributeMagnitude.Attribute(A)),
            }));

            ExpectApproximately(10f, graph.GetCurrentValue(C), "Dynamic magnitude initial value");
            Reset(evalC);

            graph.SetBaseValue(A, 20f);

            ExpectEqual(1, evalC.Count, "Dynamic dependency should dirty target while source is active");
            ExpectApproximately(20f, graph.GetCurrentValue(C), "Dynamic magnitude after dependency mutation");

            Reset(evalC);
            Expect(graph.RemoveModifierSource(handle), "Remove dynamic modifier source returned false.");

            ExpectEqual(1, evalC.Count, "Removing dynamic source should recalculate target once");
            ExpectApproximately(0f, graph.GetCurrentValue(C), "Target after dynamic source removal");

            Reset(evalC);
            graph.SetBaseValue(A, 30f);

            ExpectEqual(0, evalC.Count, "Removed dynamic dependency should not dirty target");
            ExpectApproximately(0f, graph.GetCurrentValue(C), "Target after removed dependency mutation");
        }

        private static void MagnitudeDependencyCycleRollsBackModifierSource()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Attribute(B)),
                })), "Dynamic modifier dependency cycle");

            Expect(graph.SetBaseValue(A, 3f), "SetBaseValue after dynamic cycle rollback returned false.");
            ExpectApproximately(5f, graph.GetCurrentValue(B), "B after dynamic cycle rollback");
        }

        private static void MagnitudeMissingDependenciesFailFastWithoutMutatingGraph()
        {
            var graph = new AttributeGraph();
            var evalC = new CountingEvaluator(Array.Empty<PFAttributeId>(), (_, __, raw) => raw);

            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(C, new AttributeValue(0f), evalC);

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(C, GEOperation.Add, AttributeMagnitude.Attribute(D)),
                })), "Missing magnitude dependency");

            Reset(evalC);
            graph.SetBaseValue(A, 20f);

            ExpectEqual(0, evalC.Count, "Missing dependency source should not leave target dirty edge");
            ExpectApproximately(0f, graph.GetCurrentValue(C), "Target after missing dependency failure");
        }

        private static void CycleRegistrationFailsAndRestoresPreviousGraph()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));

            ExpectThrows<InvalidOperationException>(() =>
                graph.SetEvaluator(A, new FormulaAttributeEvaluator(
                    new[] { B },
                    (context, _, raw) => raw + context.GetCurrentValue(B))), "Cycle registration");

            Expect(graph.SetBaseValue(A, 3f), "SetBaseValue after cycle rollback returned false.");
            ExpectApproximately(5f, graph.GetCurrentValue(B), "B after rollback");
        }

        private static void BatchUpdateRecalculatesOnceAtEndUsingFinalValues()
        {
            var graph = new AttributeGraph();
            var evalB = new CountingEvaluator(new[] { A }, (context, _, raw) => raw + context.GetCurrentValue(A));

            using (graph.BatchUpdate())
            {
                graph.AddAttribute(A, new AttributeValue(1f));
                graph.AddAttribute(B, new AttributeValue(10f), evalB);
                graph.SetBaseValue(A, 2f);
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(3f)),
                }));
                ExpectEqual(0, evalB.Count, "B count before BatchUpdate dispose");
            }

            ExpectEqual(1, evalB.Count, "B count after BatchUpdate dispose");
            ExpectApproximately(5f, graph.GetCurrentValue(A), "A after batch");
            ExpectApproximately(15f, graph.GetCurrentValue(B), "B after batch");
        }

        private static void BatchCycleRegistrationFailsBeforeEndBatch()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));

            using (graph.BatchUpdate())
            {
                graph.SetBaseValue(A, 3f);
                ExpectThrows<InvalidOperationException>(() =>
                    graph.SetEvaluator(A, new FormulaAttributeEvaluator(
                        new[] { B },
                        (context, _, raw) => raw + context.GetCurrentValue(B))), "Batch cycle registration");
            }

            ExpectApproximately(5f, graph.GetCurrentValue(B), "B after failed batch cycle");
        }

        private static void AttributeChangeEventsAreBatchedAfterRecalculate()
        {
            var graph = new AttributeGraph();
            var batchEvents = 0;
            var singleEvents = 0;
            AttributeChange[] lastChanges = null;

            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddAttribute(B, new AttributeValue(0f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));

            graph.AttributesChanged += changes =>
            {
                batchEvents++;
                lastChanges = changes;
            };
            graph.AttributeChanged += _ => singleEvents++;

            using (graph.BatchUpdate())
            {
                graph.SetBaseValue(A, 20f);
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
                }));
                ExpectEqual(0, batchEvents, "Batch event count before BatchUpdate dispose");
                ExpectEqual(0, singleEvents, "Single event count before BatchUpdate dispose");
            }

            ExpectEqual(1, batchEvents, "Batch event count after BatchUpdate dispose");
            ExpectEqual(2, singleEvents, "Single event count after BatchUpdate dispose");
            Expect(lastChanges != null && lastChanges.Length == 2, "Expected two changed attributes.");
            ExpectApproximately(25f, graph.GetCurrentValue(A), "A event final value");
            ExpectApproximately(25f, graph.GetCurrentValue(B), "B event final value");
        }

        private static void NonFiniteValuesAreRejected()
        {
            ExpectThrows<ArgumentException>(() =>
                new AttributeValue(float.NaN), "NaN base value");
            ExpectThrows<ArgumentException>(() =>
                new AttributeValue(1f, minValue: float.NegativeInfinity), "Infinite min value");

            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));

            ExpectThrows<ArgumentException>(() =>
                graph.SetBaseValue(A, float.PositiveInfinity), "Infinite base set");
            ExpectThrows<ArgumentException>(() =>
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(float.NaN)), "NaN modifier");
        }

        private static void RemoveAttributeRejectsLiveDependencies()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));

            ExpectThrows<InvalidOperationException>(() =>
                graph.RemoveAttribute(A), "Remove dependency target");
            Expect(graph.RemoveAttribute(B), "Remove leaf returned false.");
            Expect(graph.RemoveAttribute(A), "Remove dependency after leaf returned false.");
        }

        private static void RemoveAttributeRollbackOnRecalculationFailure()
        {
            var graph = new AttributeGraph();
            var shouldThrow = false;
            var batchEvents = 0;
            var singleEvents = 0;

            graph.AddAttribute(A, new AttributeValue(1f), new CountingEvaluator(
                Array.Empty<PFAttributeId>(),
                (_, __, raw) =>
                {
                    if (shouldThrow)
                    {
                        throw new InvalidOperationException("Intentional evaluator failure.");
                    }

                    return raw;
                }));
            graph.AddAttribute(B, new AttributeValue(2f));

            graph.AttributesChanged += _ => batchEvents++;
            graph.AttributeChanged += _ => singleEvents++;

            shouldThrow = true;
            ExpectThrows<InvalidOperationException>(() =>
                graph.RemoveAttribute(B), "Failing RemoveAttribute recalculation");

            Expect(graph.TryGetValue(B, out var value), "B should be restored after failed RemoveAttribute.");
            ExpectApproximately(2f, value.CurrentValue, "B current after failed RemoveAttribute");
            ExpectEqual(0, batchEvents, "Batch events after failed RemoveAttribute");
            ExpectEqual(0, singleEvents, "Single events after failed RemoveAttribute");

            shouldThrow = false;
            Expect(graph.RemoveAttribute(B), "RemoveAttribute should succeed after rollback.");
            Expect(!graph.TryGetValue(B, out _), "B should be removed after successful RemoveAttribute.");
        }

        private static void AttributeRegistrationRollbackUsesTransaction()
        {
            var graph = new AttributeGraph();
            var batchEvents = 0;
            var singleEvents = 0;

            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AttributesChanged += _ => batchEvents++;
            graph.AttributeChanged += _ => singleEvents++;

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddAttribute(C, new AttributeValue(1f), new FormulaAttributeEvaluator(
                    new[] { C },
                    (_, __, raw) => raw)), "Self dependency registration");

            Expect(!graph.TryGetValue(C, out _), "C should not remain after failed AddAttribute.");
            ExpectEqual(0, batchEvents, "Batch events after failed AddAttribute");
            ExpectEqual(0, singleEvents, "Single events after failed AddAttribute");

            graph.AddAttribute(C, new AttributeValue(2f));
            ExpectApproximately(2f, graph.GetCurrentValue(C), "C current after valid AddAttribute");

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddAttributes(new[]
                {
                    new AttributeRule(B, 2f),
                    new AttributeRule(D, 3f, evaluator: new FormulaAttributeEvaluator(
                        Array.Empty<PFAttributeId>(),
                        (_, __, ___) => throw new InvalidOperationException("Intentional evaluator failure."))),
                }), "Failing AddAttributes recalculation");

            Expect(!graph.TryGetValue(B, out _), "B should not remain after failed AddAttributes.");
            Expect(!graph.TryGetValue(D, out _), "D should not remain after failed AddAttributes.");

            graph.AddAttribute(D, new AttributeValue(4f));
            ExpectApproximately(4f, graph.GetCurrentValue(D), "D current after valid registration");
        }

        private static void InvalidRegistrationsFailFastWithoutMutatingUsableState()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));

            ExpectThrows<InvalidOperationException>(() =>
                graph.AddAttribute(A, new AttributeValue(2f)), "Duplicate attribute");
            ExpectThrows<InvalidOperationException>(() =>
                graph.AddAttribute(C, new AttributeValue(2f), new ClampMaxAttributeEvaluator(D)), "Missing dependency");
            ExpectThrows<InvalidOperationException>(() =>
                graph.AddModifierSource(new ModifierSource(new[]
                {
                    new AttributeModifier(C, GEOperation.Add, AttributeMagnitude.Fixed(1f)),
                })), "Missing modifier target");
            ExpectThrows<InvalidOperationException>(() =>
                graph.SetEvaluator(B, new ClampMaxAttributeEvaluator(C)), "Missing dependency on existing attribute");
            ExpectApproximately(1f, graph.GetCurrentValue(A), "A after invalid registrations");
            ExpectApproximately(3f, graph.GetCurrentValue(B), "B after invalid SetEvaluator");
            Expect(!graph.TryGetValue(C, out _), "C should not remain after invalid registration.");
        }

        private static void Reset(params CountingEvaluator[] evaluators)
        {
            for (var i = 0; i < evaluators.Length; i++)
            {
                evaluators[i].Count = 0;
            }
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void ExpectEqual(int expected, int actual, string label)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}.");
            }
        }

        private static void ExpectApproximately(float expected, float actual, string label)
        {
            if (Mathf.Abs(expected - actual) > 0.0001f)
            {
                throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}.");
            }
        }

        private static void ExpectThrows<TException>(Action action, string label)
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
                    $"{label}: expected {typeof(TException).Name}, actual {exception.GetType().Name}.",
                    exception);
            }

            throw new InvalidOperationException(
                $"{label}: expected {typeof(TException).Name}, but no exception was thrown.");
        }

        private sealed class ThrowingMagnitude : IAttributeMagnitude
        {
            public ThrowingMagnitude(IEnumerable<PFAttributeId> dependencies)
            {
                Dependencies = new List<PFAttributeId>(dependencies);
            }

            public IReadOnlyList<PFAttributeId> Dependencies { get; }

            public float Evaluate(AttributeGraphContext context)
            {
                throw new InvalidOperationException("Intentional magnitude failure.");
            }
        }

        private sealed class CountingEvaluator : IAttributeEvaluator
        {
            private readonly Func<AttributeGraphContext, PFAttributeId, float, float> formula;

            public CountingEvaluator(
                IEnumerable<PFAttributeId> dependencies,
                Func<AttributeGraphContext, PFAttributeId, float, float> formula)
            {
                Dependencies = new List<PFAttributeId>(dependencies);
                this.formula = formula;
            }

            public IReadOnlyList<PFAttributeId> Dependencies { get; }

            public int Count { get; set; }

            public float Evaluate(AttributeGraphContext context, PFAttributeId attributeId, float rawValue)
            {
                Count++;
                return formula(context, attributeId, rawValue);
            }
        }

        private sealed class MutableDependencyEvaluator : IAttributeEvaluator
        {
            private readonly Func<AttributeGraphContext, PFAttributeId, float, float> formula;

            public MutableDependencyEvaluator(
                IEnumerable<PFAttributeId> dependencies,
                Func<AttributeGraphContext, PFAttributeId, float, float> formula)
            {
                MutableDependencies = new List<PFAttributeId>(dependencies);
                this.formula = formula;
            }

            public List<PFAttributeId> MutableDependencies { get; }

            public IReadOnlyList<PFAttributeId> Dependencies => MutableDependencies;

            public float Evaluate(AttributeGraphContext context, PFAttributeId attributeId, float rawValue)
            {
                return formula(context, attributeId, rawValue);
            }
        }

    }
}
