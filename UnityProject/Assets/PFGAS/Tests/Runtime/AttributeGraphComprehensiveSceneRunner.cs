using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PFGAS.Runtime.Tests
{
    public sealed class AttributeGraphComprehensiveSceneRunner : MonoBehaviour
    {
        private const int BaseAttributeId = 9200000;
        private const PFAttributeId A = (PFAttributeId)(BaseAttributeId + 0);
        private const PFAttributeId B = (PFAttributeId)(BaseAttributeId + 1);
        private const PFAttributeId C = (PFAttributeId)(BaseAttributeId + 2);
        private const PFAttributeId D = (PFAttributeId)(BaseAttributeId + 3);

        [Header("Run")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool runCorrectnessOnStart = true;
        [SerializeField] private bool runPerformanceOnStart = true;
        [SerializeField] private bool stopOnFirstFailure;
        [SerializeField] private bool drawRuntimePanel = true;
        [SerializeField] private int maxVisibleResults = 36;

        [Header("Performance")]
        [SerializeField] private int performanceAttributeCount = 1000;
        [SerializeField] private int dependencyStride = 5;
        [SerializeField] private int modifierSourceCount = 200;
        [SerializeField] private int modifiersPerSource = 5;
        [SerializeField] private int mutationIterations = 500;
        [SerializeField] private int warmupIterations = 128;
        [SerializeField] private int operationsPerFrame = 32;
        [SerializeField] private int buildItemsPerFrame = 128;
        [SerializeField] private bool includeDynamicMagnitudePerformance = true;
        [SerializeField] private bool includeModifierChurnPerformance = true;

        [Header("Results")]
        [SerializeField] private int passedCount;
        [SerializeField] private int failedCount;
        [SerializeField] private string summary;
        [SerializeField] private string performanceReport;
        [SerializeField] private List<string> lastResults = new List<string>();

        private readonly List<PFAttributeId> performanceAttributes = new List<PFAttributeId>();
        private readonly Stopwatch stopwatch = new Stopwatch();
        private bool isRunning;
        private bool cancelRequested;

        public int PassedCount => passedCount;

        public int FailedCount => failedCount;

        public string Summary => summary;

        public string PerformanceReport => performanceReport;

        public IReadOnlyList<string> LastResults => lastResults;

        private void Start()
        {
            if (runOnStart)
            {
                RunSuite(runCorrectnessOnStart, runPerformanceOnStart);
            }
        }

        [ContextMenu("Run Comprehensive Attribute Scene Tests")]
        public void RunAll()
        {
            RunSuite(runCorrectness: true, runPerformance: true);
        }

        [ContextMenu("Run Attribute Correctness Tests")]
        public void RunCorrectnessOnly()
        {
            RunSuite(runCorrectness: true, runPerformance: false);
        }

        [ContextMenu("Run Attribute Performance Tests")]
        public void RunPerformanceOnly()
        {
            RunSuite(runCorrectness: false, runPerformance: true);
        }

        [ContextMenu("Cancel Attribute Scene Tests")]
        public void Cancel()
        {
            cancelRequested = true;
        }

        private void RunSuite(bool runCorrectness, bool runPerformance)
        {
            if (isRunning)
            {
                return;
            }

            if (Application.isPlaying)
            {
                StartCoroutine(RunSuiteCoroutine(runCorrectness, runPerformance));
                return;
            }

            RunSuiteImmediate(runCorrectness, runPerformance);
        }

        private void RunSuiteImmediate(bool runCorrectness, bool runPerformance)
        {
            isRunning = true;
            cancelRequested = false;
            ResetSuite();

            try
            {
                if (runCorrectness)
                {
                    RunCorrectnessCases();
                }

                if (runPerformance)
                {
                    RunCase("AttributeGraph performance benchmark", RunPerformanceImmediate);
                }
            }
            finally
            {
                FinishSuite();
                isRunning = false;
            }
        }

        private IEnumerator RunSuiteCoroutine(bool runCorrectness, bool runPerformance)
        {
            isRunning = true;
            cancelRequested = false;
            ResetSuite();

            if (runCorrectness)
            {
                RunCorrectnessCases();
                yield return null;
            }

            if (runPerformance && !cancelRequested)
            {
                yield return RunPerformanceCoroutine();
            }

            FinishSuite();
            isRunning = false;
        }

        private void RunCorrectnessCases()
        {
            RunBaseSceneRunnerSuite();
            if (stopOnFirstFailure && failedCount > 0)
            {
                return;
            }

            RunCase("ModifierSource snapshots input list", ModifierSourceSnapshotsInputList);
            RunCase("Nested batch update publishes once", NestedBatchUpdatePublishesOnceAtOuterEnd);
            RunCase("Batch scope exception exits batching", BatchScopeExceptionExitsBatching);
            RunCase("Attribute id buffer and remove semantics", AttributeIdsAndRemoveSemantics);
            RunCase("Event reentrancy is rejected", EventReentrancyIsRejected);
            RunCase("Event listener can queue later batch work", EventListenerCanQueueLaterBatchWork);
            RunCase("AddBaseValue clamps and reports change", AddBaseValueClampsAndReportsChange);
            RunCase("Override removal restores stacking", OverrideRemovalRestoresStackingValue);
            RunCase("Duplicate dependency reference counts survive removal", DuplicateDependencyReferenceCountsSurviveModifierRemoval);
            RunCase("Active modifiers block attribute removal", ActiveModifiersBlockAttributeRemoval);
            RunCase("ModifierSourceHandle semantics", ModifierSourceHandleSemantics);
            RunCase("Deterministic reference model", DeterministicReferenceModelMatchesGraph);
        }

        private void RunBaseSceneRunnerSuite()
        {
            GameObject runnerObject = null;
            try
            {
                runnerObject = new GameObject("PFGAS AttributeGraph Base Scene Test Runner");
                runnerObject.hideFlags = HideFlags.HideAndDontSave;
                var runner = runnerObject.AddComponent<AttributeGraphSceneTestRunner>();
                runner.RunAll();

                for (var i = 0; i < runner.LastResults.Count; i++)
                {
                    var result = runner.LastResults[i];
                    if (result.StartsWith("[PASS]", StringComparison.Ordinal))
                    {
                        passedCount++;
                    }
                    else
                    {
                        failedCount++;
                    }

                    lastResults.Add("[BASE] " + result);
                }
            }
            catch (Exception exception)
            {
                failedCount++;
                lastResults.Add("[FAIL] Base AttributeGraphSceneTestRunner: " + exception.Message);
                Debug.LogException(exception, this);
            }
            finally
            {
                DestroyRunnerObject(runnerObject);
            }
        }

        private void RunCase(string caseName, Action action)
        {
            if (cancelRequested)
            {
                return;
            }

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
                    cancelRequested = true;
                }
            }
        }

        private void RunPerformanceImmediate()
        {
            NormalizePerformanceSettings();

            stopwatch.Restart();
            var graph = BuildPerformanceGraph();
            stopwatch.Stop();
            var buildMs = stopwatch.Elapsed.TotalMilliseconds;

            Warmup(graph);

            var fullRecalculateMs = MeasureFullRecalculate(graph);
            var rootMutationMs = MeasureMutations(graph, performanceAttributes[0]);
            var middleMutationMs = MeasureMutations(graph, performanceAttributes[performanceAttributes.Count / 2]);
            var leafMutationMs = MeasureMutations(graph, performanceAttributes[performanceAttributes.Count - 1]);
            var modifierChurnMs = includeModifierChurnPerformance
                ? MeasureModifierChurn(graph)
                : 0d;

            performanceReport = BuildPerformanceReport(
                buildMs,
                fullRecalculateMs,
                rootMutationMs,
                middleMutationMs,
                leafMutationMs,
                modifierChurnMs,
                graph);
            Debug.Log(performanceReport, this);
        }

        private IEnumerator RunPerformanceCoroutine()
        {
            NormalizePerformanceSettings();
            summary = "Running AttributeGraph performance benchmark...";

            var graph = new AttributeGraph();
            stopwatch.Restart();
            yield return BuildPerformanceGraphCoroutine(graph);
            stopwatch.Stop();
            var buildMs = stopwatch.Elapsed.TotalMilliseconds;
            if (cancelRequested)
            {
                performanceReport = "AttributeGraph performance benchmark canceled.";
                yield break;
            }

            yield return WarmupCoroutine(graph);

            var fullRecalculateMs = MeasureFullRecalculate(graph);
            yield return null;

            var rootMutationMs = 0d;
            yield return MeasureMutationsCoroutine(
                graph,
                performanceAttributes[0],
                "root mutation",
                value => rootMutationMs = value);

            var middleMutationMs = 0d;
            yield return MeasureMutationsCoroutine(
                graph,
                performanceAttributes[performanceAttributes.Count / 2],
                "middle mutation",
                value => middleMutationMs = value);

            var leafMutationMs = 0d;
            yield return MeasureMutationsCoroutine(
                graph,
                performanceAttributes[performanceAttributes.Count - 1],
                "leaf mutation",
                value => leafMutationMs = value);

            var modifierChurnMs = 0d;
            if (includeModifierChurnPerformance)
            {
                yield return MeasureModifierChurnCoroutine(graph, value => modifierChurnMs = value);
            }

            if (cancelRequested)
            {
                performanceReport = "AttributeGraph performance benchmark canceled.";
                yield break;
            }

            performanceReport = BuildPerformanceReport(
                buildMs,
                fullRecalculateMs,
                rootMutationMs,
                middleMutationMs,
                leafMutationMs,
                modifierChurnMs,
                graph);
            passedCount++;
            lastResults.Add("[PASS] AttributeGraph performance benchmark");
            Debug.Log(performanceReport, this);
        }

        private AttributeGraph BuildPerformanceGraph()
        {
            var graph = new AttributeGraph();
            performanceAttributes.Clear();
            using (graph.BatchUpdate())
            {
                for (var i = 0; i < performanceAttributeCount; i++)
                {
                    AddPerformanceAttribute(graph, i);
                }

                for (var sourceIndex = 0; sourceIndex < modifierSourceCount; sourceIndex++)
                {
                    graph.AddModifierSource(CreatePerformanceModifierSource(sourceIndex));
                }
            }

            return graph;
        }

        private IEnumerator BuildPerformanceGraphCoroutine(AttributeGraph graph)
        {
            performanceAttributes.Clear();
            var builtItems = 0;
            using (graph.BatchUpdate())
            {
                for (var i = 0; i < performanceAttributeCount; i++)
                {
                    AddPerformanceAttribute(graph, i);
                    if (++builtItems >= buildItemsPerFrame)
                    {
                        builtItems = 0;
                        summary = $"Building attributes {i + 1}/{performanceAttributeCount}";
                        yield return null;
                        if (cancelRequested)
                        {
                            yield break;
                        }
                    }
                }

                for (var sourceIndex = 0; sourceIndex < modifierSourceCount; sourceIndex++)
                {
                    graph.AddModifierSource(CreatePerformanceModifierSource(sourceIndex));
                    if (++builtItems >= buildItemsPerFrame)
                    {
                        builtItems = 0;
                        summary = $"Building modifier sources {sourceIndex + 1}/{modifierSourceCount}";
                        yield return null;
                        if (cancelRequested)
                        {
                            yield break;
                        }
                    }
                }
            }
        }

        private void AddPerformanceAttribute(AttributeGraph graph, int index)
        {
            var attributeId = ToPerformanceAttributeId(index);
            performanceAttributes.Add(attributeId);

            var value = new AttributeValue(
                100f + index % 37,
                AggregationMode.Stacking,
                0f,
                1000000f);

            if (index >= dependencyStride)
            {
                var dependencyA = ToPerformanceAttributeId(index - 1);
                var dependencyB = ToPerformanceAttributeId(index - dependencyStride);
                graph.AddAttribute(
                    attributeId,
                    value,
                    new FormulaAttributeEvaluator(
                        new[] { dependencyA, dependencyB },
                        (context, _, rawValue) =>
                            rawValue +
                            context.GetCurrentValue(dependencyA) * 0.01f +
                            context.GetCurrentValue(dependencyB) * 0.005f));
                return;
            }

            graph.AddAttribute(attributeId, value);
        }

        private ModifierSource CreatePerformanceModifierSource(int sourceIndex)
        {
            var modifiers = new AttributeModifier[modifiersPerSource];
            for (var i = 0; i < modifiers.Length; i++)
            {
                var attributeIndex = Math.Abs((sourceIndex * 31 + i * 17) % performanceAttributeCount);
                var operation = i % 3 == 0 ? GEOperation.Multiply : GEOperation.Add;
                modifiers[i] = new AttributeModifier(
                    ToPerformanceAttributeId(attributeIndex),
                    operation,
                    CreatePerformanceMagnitude(attributeIndex, operation, i));
            }

            return new ModifierSource("comprehensive-perf-source-" + sourceIndex, modifiers);
        }

        private IAttributeMagnitude CreatePerformanceMagnitude(
            int attributeIndex,
            GEOperation operation,
            int modifierIndex)
        {
            var fixedValue = operation == GEOperation.Multiply ? 1.001f : 1f + modifierIndex;
            if (!includeDynamicMagnitudePerformance || attributeIndex <= 0)
            {
                return AttributeMagnitude.Fixed(fixedValue);
            }

            var dependency = ToPerformanceAttributeId(attributeIndex - 1);
            if (operation == GEOperation.Multiply)
            {
                return AttributeMagnitude.Add(
                    AttributeMagnitude.Fixed(1f),
                    AttributeMagnitude.Clamp(
                        AttributeMagnitude.Divide(
                            AttributeMagnitude.Attribute(dependency),
                            AttributeMagnitude.Fixed(100000f)),
                        AttributeMagnitude.Fixed(0f),
                        AttributeMagnitude.Fixed(0.01f)));
            }

            return AttributeMagnitude.Clamp(
                AttributeMagnitude.Add(
                    AttributeMagnitude.Fixed(fixedValue),
                    AttributeMagnitude.Multiply(
                        AttributeMagnitude.Attribute(dependency),
                        AttributeMagnitude.Fixed(0.01f))),
                AttributeMagnitude.Fixed(0f),
                AttributeMagnitude.Fixed(100000f));
        }

        private void Warmup(AttributeGraph graph)
        {
            for (var i = 0; i < warmupIterations; i++)
            {
                graph.AddBaseValue(performanceAttributes[i % performanceAttributes.Count], 0.01f);
            }
        }

        private IEnumerator WarmupCoroutine(AttributeGraph graph)
        {
            for (var i = 0; i < warmupIterations; i++)
            {
                graph.AddBaseValue(performanceAttributes[i % performanceAttributes.Count], 0.01f);
                if (i % operationsPerFrame == operationsPerFrame - 1)
                {
                    summary = $"Warming up {i + 1}/{warmupIterations}";
                    yield return null;
                }
            }
        }

        private double MeasureFullRecalculate(AttributeGraph graph)
        {
            stopwatch.Restart();
            graph.RecalculateAll();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private double MeasureMutations(AttributeGraph graph, PFAttributeId attributeId)
        {
            stopwatch.Restart();
            for (var i = 0; i < mutationIterations; i++)
            {
                graph.AddBaseValue(attributeId, 1f);
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private IEnumerator MeasureMutationsCoroutine(
            AttributeGraph graph,
            PFAttributeId attributeId,
            string label,
            Action<double> setResult)
        {
            stopwatch.Reset();
            for (var i = 0; i < mutationIterations; i++)
            {
                stopwatch.Start();
                var end = Math.Min(i + operationsPerFrame, mutationIterations);
                for (; i < end; i++)
                {
                    graph.AddBaseValue(attributeId, 1f);
                }

                i--;
                stopwatch.Stop();
                summary = $"Measuring {label} {i + 1}/{mutationIterations}";
                yield return null;
                if (cancelRequested)
                {
                    break;
                }
            }

            setResult(stopwatch.Elapsed.TotalMilliseconds);
        }

        private double MeasureModifierChurn(AttributeGraph graph)
        {
            stopwatch.Restart();
            for (var i = 0; i < mutationIterations; i++)
            {
                var handle = graph.AddModifierSource(CreatePerformanceModifierSource(modifierSourceCount + i));
                Expect(graph.RemoveModifierSource(handle), "Modifier churn remove failed.");
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private IEnumerator MeasureModifierChurnCoroutine(AttributeGraph graph, Action<double> setResult)
        {
            stopwatch.Reset();
            for (var i = 0; i < mutationIterations; i++)
            {
                stopwatch.Start();
                var end = Math.Min(i + operationsPerFrame, mutationIterations);
                for (; i < end; i++)
                {
                    var handle = graph.AddModifierSource(CreatePerformanceModifierSource(modifierSourceCount + i));
                    Expect(graph.RemoveModifierSource(handle), "Modifier churn remove failed.");
                }

                i--;
                stopwatch.Stop();
                summary = $"Measuring modifier churn {i + 1}/{mutationIterations}";
                yield return null;
                if (cancelRequested)
                {
                    break;
                }
            }

            setResult(stopwatch.Elapsed.TotalMilliseconds);
        }

        private string BuildPerformanceReport(
            double buildMs,
            double fullRecalculateMs,
            double rootMutationMs,
            double middleMutationMs,
            double leafMutationMs,
            double modifierChurnMs,
            AttributeGraph graph)
        {
            var sampleValue = graph.GetCurrentValue(performanceAttributes[performanceAttributes.Count - 1]);
            ExpectFinite(sampleValue, "Performance sample value");

            var sb = new StringBuilder();
            sb.AppendLine("PFGAS AttributeGraph Comprehensive Performance");
            sb.AppendLine($"Attributes: {performanceAttributeCount}, DependencyStride: {dependencyStride}");
            sb.AppendLine($"ModifierSources: {modifierSourceCount}, Modifiers/Source: {modifiersPerSource}");
            sb.AppendLine($"DynamicMagnitude: {includeDynamicMagnitudePerformance}, ModifierChurn: {includeModifierChurnPerformance}");
            sb.AppendLine($"Mutations: {mutationIterations}, Warmup: {warmupIterations}");
            sb.AppendLine($"Batch Build: {buildMs:F3} ms");
            sb.AppendLine($"Full Recalculate: {fullRecalculateMs:F3} ms");
            sb.AppendLine($"Root Mutation: {rootMutationMs:F3} ms, Avg {rootMutationMs / mutationIterations:F6} ms");
            sb.AppendLine($"Middle Mutation: {middleMutationMs:F3} ms, Avg {middleMutationMs / mutationIterations:F6} ms");
            sb.AppendLine($"Leaf Mutation: {leafMutationMs:F3} ms, Avg {leafMutationMs / mutationIterations:F6} ms");
            if (includeModifierChurnPerformance)
            {
                sb.AppendLine($"Modifier Add+Remove: {modifierChurnMs:F3} ms, Avg {modifierChurnMs / mutationIterations:F6} ms");
            }

            sb.AppendLine($"Sample Last Attribute: {sampleValue:F3}");
            return sb.ToString();
        }

        private static void ModifierSourceSnapshotsInputList()
        {
            var modifiers = new List<AttributeModifier>
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
            };
            var source = new ModifierSource("snapshot", modifiers);
            modifiers.Clear();

            ExpectEqual(1, source.Modifiers.Count, "Source modifier snapshot count");
            Expect(!(source.Modifiers is List<AttributeModifier>), "Source modifiers should not expose a mutable List.");

            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(10f));
            graph.AddModifierSource(source);

            ExpectApproximately(15f, graph.GetCurrentValue(A), "Graph uses source snapshot");
        }

        private static void NestedBatchUpdatePublishesOnceAtOuterEnd()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(10f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));
            graph.AddAttribute(C, new AttributeValue(100f), new FormulaAttributeEvaluator(
                new[] { B },
                (context, _, raw) => raw + context.GetCurrentValue(B)));

            var batchEventCount = 0;
            var singleEventCount = 0;
            AttributeChange[] lastChanges = null;
            graph.AttributesChanged += changes =>
            {
                batchEventCount++;
                lastChanges = changes;
            };
            graph.AttributeChanged += _ => singleEventCount++;

            using (graph.BatchUpdate())
            {
                using (graph.BatchUpdate())
                {
                    graph.SetBaseValue(A, 2f);
                    graph.AddModifierSource(new ModifierSource(new[]
                    {
                        new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(3f)),
                    }));
                }

                ExpectEqual(0, batchEventCount, "Inner BatchUpdate dispose batch events");
                ExpectEqual(0, singleEventCount, "Inner BatchUpdate dispose single events");
            }

            ExpectEqual(1, batchEventCount, "Outer BatchUpdate dispose batch events");
            ExpectEqual(3, singleEventCount, "Outer BatchUpdate dispose single events");
            Expect(lastChanges != null && lastChanges.Length == 3, "Expected A/B/C change records.");
            ExpectApproximately(5f, graph.GetCurrentValue(A), "Nested batch A");
            ExpectApproximately(15f, graph.GetCurrentValue(B), "Nested batch B");
            ExpectApproximately(115f, graph.GetCurrentValue(C), "Nested batch C");
        }

        private static void BatchScopeExceptionExitsBatching()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(10f), new FormulaAttributeEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A)));

            var batchEventCount = 0;
            graph.AttributesChanged += _ => batchEventCount++;

            ExpectThrows<InvalidOperationException>(() =>
            {
                using (graph.BatchUpdate())
                {
                    graph.SetBaseValue(A, 2f);
                    throw new InvalidOperationException("Simulated batch body failure.");
                }
            }, "Batch scope body exception");

            ExpectEqual(1, batchEventCount, "Batch scope should dispose and publish once despite body exception");
            ExpectApproximately(12f, graph.GetCurrentValue(B), "B after exception batch dispose");

            graph.SetBaseValue(A, 3f);
            ExpectEqual(2, batchEventCount, "Graph should not remain batching after exception");
            ExpectApproximately(13f, graph.GetCurrentValue(B), "B after post-exception mutation");
        }

        private static void AttributeIdsAndRemoveSemantics()
        {
            var missing = (PFAttributeId)(BaseAttributeId + 4);
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(2f), new ClampMinAttributeEvaluator(A));
            graph.AddAttribute(C, new AttributeValue(3f));

            ExpectEqual(3, graph.Count, "Initial graph count");
            var attributeIds = new List<PFAttributeId>();
            graph.GetAttributeIds(attributeIds);
            Expect(ContainsAttributeId(attributeIds, A), "Attribute ids missing A.");
            Expect(ContainsAttributeId(attributeIds, B), "Attribute ids missing B.");
            Expect(ContainsAttributeId(attributeIds, C), "Attribute ids missing C.");

            graph.AddAttribute(D, new AttributeValue(4f));
            Expect(!ContainsAttributeId(attributeIds, D), "Attribute id snapshot should not change after graph mutation.");
            graph.GetAttributeIds(attributeIds);
            Expect(ContainsAttributeId(attributeIds, D), "Refreshed attribute ids missing D.");
            Expect(!graph.RemoveAttribute(missing), "Removing missing attribute should return false.");
            ExpectThrows<InvalidOperationException>(() => graph.RemoveAttribute(A), "Remove live dependency");
            Expect(graph.RemoveAttribute(B), "Remove dependent leaf failed.");
            Expect(graph.RemoveAttribute(A), "Remove dependency after dependent failed.");
            Expect(graph.RemoveAttribute(C), "Remove independent attribute failed.");
            Expect(graph.RemoveAttribute(D), "Remove snapshot-added attribute failed.");
            ExpectEqual(0, graph.Count, "Graph count after removals");
        }

        private static void EventReentrancyIsRejected()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));

            var mutateInListener = true;
            graph.AttributeChanged += _ =>
            {
                if (mutateInListener)
                {
                    graph.SetBaseValue(A, 3f);
                }
            };

            ExpectThrows<InvalidOperationException>(() =>
                graph.SetBaseValue(A, 2f), "Same graph mutation during AttributeChanged");

            mutateInListener = false;
            Expect(graph.SetBaseValue(A, 4f), "Graph should unlock after rejected event reentrancy.");
            ExpectApproximately(4f, graph.GetCurrentValue(A), "A after event guard recovery");
        }

        private static void EventListenerCanQueueLaterBatchWork()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));

            var pendingBuff = false;
            var pendingBuffCollected = false;
            graph.AttributesChanged += _ =>
            {
                if (!pendingBuffCollected)
                {
                    pendingBuffCollected = true;
                    pendingBuff = true;
                }
            };

            graph.SetBaseValue(A, 2f);
            Expect(pendingBuff, "Listener should collect pending buff work.");
            ExpectApproximately(2f, graph.GetCurrentValue(A), "A before pending buff");

            using (graph.BatchUpdate())
            {
                if (pendingBuff)
                {
                    pendingBuff = false;
                    graph.AddModifierSource(new ModifierSource(new[]
                    {
                        new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
                    }));
                }
            }

            ExpectApproximately(7f, graph.GetCurrentValue(A), "A after queued batch work");
        }

        private static void AddBaseValueClampsAndReportsChange()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(5f, minValue: 0f, maxValue: 10f));

            Expect(graph.AddBaseValue(A, 100f), "AddBaseValue should report clamped change.");
            ExpectApproximately(10f, graph.GetBaseValue(A), "Clamped base value");
            ExpectApproximately(10f, graph.GetCurrentValue(A), "Clamped current value");
            Expect(!graph.SetBaseValue(A, 999f), "SetBaseValue should report no change when clamp result is unchanged.");
        }

        private static void OverrideRemovalRestoresStackingValue()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(10f));

            var stacking = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(5f)),
                new AttributeModifier(A, GEOperation.Multiply, AttributeMagnitude.Fixed(2f)),
            }));
            var overrideHandle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Override, AttributeMagnitude.Fixed(7f)),
            }));

            ExpectApproximately(7f, graph.GetCurrentValue(A), "Override current value");
            Expect(graph.RemoveModifierSource(overrideHandle), "Remove override failed.");
            ExpectApproximately(30f, graph.GetCurrentValue(A), "Stacking restored after override removal");
            Expect(graph.RemoveModifierSource(stacking), "Remove stacking failed.");
            ExpectApproximately(10f, graph.GetCurrentValue(A), "Base restored after stacking removal");
        }

        private static void DuplicateDependencyReferenceCountsSurviveModifierRemoval()
        {
            var graph = new AttributeGraph();
            var evaluator = new CountingEvaluator(
                new[] { A },
                (context, _, raw) => raw + context.GetCurrentValue(A));

            graph.AddAttribute(A, new AttributeValue(1f));
            graph.AddAttribute(B, new AttributeValue(10f), evaluator);

            var handle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(B, GEOperation.Add, AttributeMagnitude.Attribute(A)),
            }));
            ExpectApproximately(12f, graph.GetCurrentValue(B), "B while dynamic modifier is active");

            Reset(evaluator);
            Expect(graph.RemoveModifierSource(handle), "Remove dynamic modifier failed.");
            ExpectApproximately(11f, graph.GetCurrentValue(B), "B after dynamic modifier removal");

            Reset(evaluator);
            graph.SetBaseValue(A, 2f);

            ExpectEqual(1, evaluator.Count, "Evaluator dependency edge should remain after modifier removal");
            ExpectApproximately(12f, graph.GetCurrentValue(B), "B after evaluator dependency mutation");
        }

        private static void ActiveModifiersBlockAttributeRemoval()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));
            var handle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(1f)),
            }));

            ExpectThrows<InvalidOperationException>(() => graph.RemoveAttribute(A), "Remove active modifier target");
            Expect(graph.RemoveModifierSource(handle), "Remove active modifier failed.");
            Expect(graph.RemoveAttribute(A), "Remove attribute after modifier removal failed.");
        }

        private static void ModifierSourceHandleSemantics()
        {
            var graph = new AttributeGraph();
            graph.AddAttribute(A, new AttributeValue(1f));

            Expect(!ModifierSourceHandle.Invalid.IsValid, "Invalid handle should be invalid.");
            Expect(!graph.RemoveModifierSource(ModifierSourceHandle.Invalid), "Removing invalid handle should return false.");

            var handle = graph.AddModifierSource(new ModifierSource(new[]
            {
                new AttributeModifier(A, GEOperation.Add, AttributeMagnitude.Fixed(1f)),
            }));

            Expect(handle.IsValid, "Added handle should be valid.");
            Expect(graph.RemoveModifierSource(handle), "First remove should return true.");
            Expect(!graph.RemoveModifierSource(handle), "Second remove should return false.");
        }

        private static void DeterministicReferenceModelMatchesGraph()
        {
            const int count = 32;
            var graph = new AttributeGraph();
            var ids = new PFAttributeId[count];
            var baseValues = new float[count];
            var fixedAdds = new float[count];
            var multipliers = new float[count];
            var dynamicAddCoefficients = new float[count];
            var modifiers = new List<AttributeModifier>();

            for (var i = 0; i < count; i++)
            {
                ids[i] = (PFAttributeId)(BaseAttributeId + 100 + i);
                baseValues[i] = 10f + i;
                multipliers[i] = 1f;
            }

            using (graph.BatchUpdate())
            {
                for (var i = 0; i < count; i++)
                {
                    var index = i;
                    if (i >= 2)
                    {
                        var dependencyA = ids[i - 1];
                        var dependencyB = ids[i - 2];
                        graph.AddAttribute(
                            ids[i],
                            new AttributeValue(baseValues[i], minValue: 0f, maxValue: 1000000f),
                            new FormulaAttributeEvaluator(
                                new[] { dependencyA, dependencyB },
                                (context, _, raw) =>
                                    raw +
                                    context.GetCurrentValue(dependencyA) * 0.1f +
                                    context.GetCurrentValue(dependencyB) * 0.05f));
                    }
                    else
                    {
                        graph.AddAttribute(ids[index], new AttributeValue(baseValues[index], minValue: 0f, maxValue: 1000000f));
                    }
                }

                for (var i = 0; i < count; i++)
                {
                    if (i % 3 == 0)
                    {
                        var add = 1f + i * 0.25f;
                        fixedAdds[i] += add;
                        modifiers.Add(new AttributeModifier(ids[i], GEOperation.Add, AttributeMagnitude.Fixed(add)));
                    }

                    if (i % 5 == 0)
                    {
                        const float multiply = 1.05f;
                        multipliers[i] *= multiply;
                        modifiers.Add(new AttributeModifier(ids[i], GEOperation.Multiply, AttributeMagnitude.Fixed(multiply)));
                    }

                    if (i > 0 && i % 4 == 0)
                    {
                        const float coefficient = 0.1f;
                        dynamicAddCoefficients[i] += coefficient;
                        modifiers.Add(new AttributeModifier(
                            ids[i],
                            GEOperation.Add,
                            AttributeMagnitude.Multiply(
                                AttributeMagnitude.Attribute(ids[i - 1]),
                                AttributeMagnitude.Fixed(coefficient))));
                    }
                }

                graph.AddModifierSource(new ModifierSource("reference-model", modifiers));
            }

            var expected = CalculateReferenceValues(baseValues, fixedAdds, multipliers, dynamicAddCoefficients);
            AssertGraphMatchesReference(graph, ids, expected, "initial reference model");

            var changedIndices = new[] { 0, 3, 8, 17 };
            for (var i = 0; i < changedIndices.Length; i++)
            {
                var index = changedIndices[i];
                baseValues[index] += 7f + i;
                graph.SetBaseValue(ids[index], baseValues[index]);
            }

            expected = CalculateReferenceValues(baseValues, fixedAdds, multipliers, dynamicAddCoefficients);
            AssertGraphMatchesReference(graph, ids, expected, "mutated reference model");
        }

        private static float[] CalculateReferenceValues(
            IReadOnlyList<float> baseValues,
            IReadOnlyList<float> fixedAdds,
            IReadOnlyList<float> multipliers,
            IReadOnlyList<float> dynamicAddCoefficients)
        {
            var values = new float[baseValues.Count];
            for (var i = 0; i < values.Length; i++)
            {
                var dynamicAdd = i > 0 ? values[i - 1] * dynamicAddCoefficients[i] : 0f;
                var raw = (baseValues[i] + fixedAdds[i] + dynamicAdd) * multipliers[i];
                values[i] = i >= 2
                    ? raw + values[i - 1] * 0.1f + values[i - 2] * 0.05f
                    : raw;
            }

            return values;
        }

        private static void AssertGraphMatchesReference(
            AttributeGraph graph,
            IReadOnlyList<PFAttributeId> ids,
            IReadOnlyList<float> expected,
            string label)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                ExpectApproximately(expected[i], graph.GetCurrentValue(ids[i]), $"{label} attribute {i}");
            }
        }

        private void ResetSuite()
        {
            passedCount = 0;
            failedCount = 0;
            summary = "Running AttributeGraph comprehensive scene tests...";
            performanceReport = string.Empty;
            lastResults.Clear();
        }

        private void FinishSuite()
        {
            summary = cancelRequested
                ? $"PFGAS AttributeGraph comprehensive scene tests canceled. Passed: {passedCount}, Failed: {failedCount}."
                : $"PFGAS AttributeGraph comprehensive scene tests finished. Passed: {passedCount}, Failed: {failedCount}.";

            if (failedCount == 0 && !cancelRequested)
            {
                Debug.Log(summary, this);
            }
            else
            {
                Debug.LogWarning(summary, this);
            }
        }

        private void NormalizePerformanceSettings()
        {
            performanceAttributeCount = Math.Max(2, performanceAttributeCount);
            dependencyStride = Math.Max(1, dependencyStride);
            modifierSourceCount = Math.Max(0, modifierSourceCount);
            modifiersPerSource = Math.Max(1, modifiersPerSource);
            mutationIterations = Math.Max(1, mutationIterations);
            warmupIterations = Math.Max(0, warmupIterations);
            operationsPerFrame = Math.Max(1, operationsPerFrame);
            buildItemsPerFrame = Math.Max(1, buildItemsPerFrame);
        }

        private void OnGUI()
        {
            if (!drawRuntimePanel || string.IsNullOrEmpty(summary))
            {
                return;
            }

            var width = Mathf.Min(Screen.width - 24f, 1060f);
            var height = Mathf.Min(Screen.height - 24f, 680f);
            GUILayout.BeginArea(new Rect(12f, 12f, width, height), GUI.skin.box);
            GUILayout.Label(summary);

            if (!string.IsNullOrEmpty(performanceReport))
            {
                GUILayout.Label(performanceReport);
            }

            var visibleCount = Math.Min(maxVisibleResults, lastResults.Count);
            var start = Math.Max(0, lastResults.Count - visibleCount);
            for (var i = start; i < lastResults.Count; i++)
            {
                GUILayout.Label(lastResults[i]);
            }

            GUILayout.EndArea();
        }

        private static bool ContainsAttributeId(IEnumerable<PFAttributeId> ids, PFAttributeId expected)
        {
            foreach (var id in ids)
            {
                if (id == expected)
                {
                    return true;
                }
            }

            return false;
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

        private static void ExpectFinite(float value, string label)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidOperationException($"{label}: expected finite value, actual {value}.");
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

        private static void DestroyRunnerObject(GameObject runnerObject)
        {
            if (runnerObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runnerObject);
            }
            else
            {
                DestroyImmediate(runnerObject);
            }
        }

        private static PFAttributeId ToPerformanceAttributeId(int index)
        {
            return (PFAttributeId)(BaseAttributeId + 10000 + index);
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
    }
}
