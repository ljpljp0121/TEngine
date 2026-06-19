using System.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PFGAS.Runtime.Tests
{
    public sealed class AttributeGraphPerformanceRunner : MonoBehaviour
    {
        [SerializeField] private bool runOnStart;
        [SerializeField] private bool drawRuntimePanel = true;
        [SerializeField] private int attributeCount = 500;
        [SerializeField] private int dependencyStride = 4;
        [SerializeField] private int modifierSourceCount = 100;
        [SerializeField] private int modifiersPerSource = 4;
        [SerializeField] private int mutationIterations = 200;
        [SerializeField] private int warmupIterations = 64;
        [SerializeField] private int operationsPerFrame = 16;
        [SerializeField] private int buildItemsPerFrame = 64;
        [SerializeField] private bool benchmarkModifierAddRemove;
        [SerializeField] private bool useDynamicMagnitudeModifiers;
        [SerializeField] private string lastReport;

        private readonly List<PFAttributeId> attributes = new List<PFAttributeId>();
        private readonly Stopwatch stopwatch = new Stopwatch();
        private bool isRunning;
        private bool cancelRequested;

        public string LastReport => lastReport;

        private void Start()
        {
            if (runOnStart)
            {
                if (IsLargeBenchmark())
                {
                    lastReport =
                        "AttributeGraph benchmark is large, skipped RunOnStart.\n" +
                        "Use the component context menu to run it manually.";
                    Debug.LogWarning(lastReport, this);
                    return;
                }

                RunBenchmark();
            }
        }

        [ContextMenu("Run AttributeGraph Benchmark")]
        public void RunBenchmark()
        {
            if (isRunning)
            {
                return;
            }

            if (Application.isPlaying)
            {
                StartCoroutine(RunBenchmarkCoroutine());
                return;
            }

            RunBenchmarkImmediate();
        }

        [ContextMenu("Cancel AttributeGraph Benchmark")]
        public void CancelBenchmark()
        {
            cancelRequested = true;
        }

        private void RunBenchmarkImmediate()
        {
            attributeCount = Mathf.Max(2, attributeCount);
            dependencyStride = Mathf.Max(1, dependencyStride);
            modifierSourceCount = Mathf.Max(0, modifierSourceCount);
            modifiersPerSource = Mathf.Max(1, modifiersPerSource);
            mutationIterations = Mathf.Max(1, mutationIterations);
            warmupIterations = Mathf.Max(0, warmupIterations);

            stopwatch.Restart();
            var graph = BuildGraph();
            stopwatch.Stop();
            var buildMs = stopwatch.Elapsed.TotalMilliseconds;
            Warmup(graph);

            var fullRecalculateMs = MeasureFullRecalculate(graph);
            var rootMutationMs = MeasureMutations(graph, attributes[0]);
            var middleMutationMs = MeasureMutations(graph, attributes[attributes.Count / 2]);
            var leafMutationMs = MeasureMutations(graph, attributes[attributes.Count - 1]);
            var modifierAddRemoveMs = benchmarkModifierAddRemove
                ? MeasureModifierAddRemove(graph)
                : 0d;

            lastReport = BuildReport(
                buildMs,
                fullRecalculateMs,
                rootMutationMs,
                middleMutationMs,
                leafMutationMs,
                modifierAddRemoveMs,
                graph);
            Debug.Log(lastReport, this);
        }

        private IEnumerator RunBenchmarkCoroutine()
        {
            isRunning = true;
            cancelRequested = false;
            NormalizeSettings();

            var graph = new AttributeGraph();
            stopwatch.Restart();
            yield return BuildGraphCoroutine(graph);
            stopwatch.Stop();
            var buildMs = stopwatch.Elapsed.TotalMilliseconds;
            if (cancelRequested)
            {
                FinishCanceled();
                yield break;
            }

            yield return WarmupCoroutine(graph);
            if (cancelRequested)
            {
                FinishCanceled();
                yield break;
            }

            var fullRecalculateMs = MeasureFullRecalculate(graph);
            yield return null;

            var rootMutationMs = 0d;
            yield return MeasureMutationsCoroutine(
                graph,
                attributes[0],
                "root mutations",
                value => rootMutationMs = value);
            if (cancelRequested)
            {
                FinishCanceled();
                yield break;
            }

            var middleMutationMs = 0d;
            yield return MeasureMutationsCoroutine(
                graph,
                attributes[attributes.Count / 2],
                "middle mutations",
                value => middleMutationMs = value);
            if (cancelRequested)
            {
                FinishCanceled();
                yield break;
            }

            var leafMutationMs = 0d;
            yield return MeasureMutationsCoroutine(
                graph,
                attributes[attributes.Count - 1],
                "leaf mutations",
                value => leafMutationMs = value);
            if (cancelRequested)
            {
                FinishCanceled();
                yield break;
            }

            var modifierAddRemoveMs = 0d;
            if (benchmarkModifierAddRemove)
            {
                yield return MeasureModifierAddRemoveCoroutine(graph, value => modifierAddRemoveMs = value);
            }

            lastReport = BuildReport(
                buildMs,
                fullRecalculateMs,
                rootMutationMs,
                middleMutationMs,
                leafMutationMs,
                modifierAddRemoveMs,
                graph);
            Debug.Log(lastReport, this);
            isRunning = false;
        }

        private void OnGUI()
        {
            if (!drawRuntimePanel || string.IsNullOrEmpty(lastReport))
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 760f, 240f), GUI.skin.box);
            GUILayout.Label(lastReport);
            GUILayout.EndArea();
        }

        private AttributeGraph BuildGraph()
        {
            attributes.Clear();
            var graph = new AttributeGraph();
            using (graph.BatchUpdate())
            {
                for (var i = 0; i < attributeCount; i++)
                {
                    var attributeId = ToAttributeId(i);
                    attributes.Add(attributeId);

                    var value = new AttributeValue(
                        baseValue: 100f + i % 37,
                        aggregationMode: AggregationMode.Stacking,
                        minValue: 0f,
                        maxValue: 1000000f);

                    if (i >= dependencyStride)
                    {
                        var dependencyA = ToAttributeId(i - 1);
                        var dependencyB = ToAttributeId(i - dependencyStride);
                        graph.AddAttribute(
                            attributeId,
                            value,
                            new FormulaAttributeEvaluator(
                                new[] { dependencyA, dependencyB },
                                (context, _, rawValue) =>
                                    rawValue +
                                    context.GetCurrentValue(dependencyA) * 0.01f +
                                    context.GetCurrentValue(dependencyB) * 0.005f));
                    }
                    else
                    {
                        graph.AddAttribute(attributeId, value);
                    }
                }

                for (var sourceIndex = 0; sourceIndex < modifierSourceCount; sourceIndex++)
                {
                    graph.AddModifierSource(CreateModifierSource(sourceIndex));
                }
            }

            return graph;
        }

        private IEnumerator BuildGraphCoroutine(AttributeGraph graph)
        {
            attributes.Clear();
            var builtItems = 0;
            using (graph.BatchUpdate())
            {
                for (var i = 0; i < attributeCount; i++)
                {
                    AddAttribute(graph, i);
                    if (++builtItems >= buildItemsPerFrame)
                    {
                        builtItems = 0;
                        lastReport = $"Building attributes {i + 1}/{attributeCount}";
                        yield return null;
                        if (cancelRequested)
                        {
                            yield break;
                        }
                    }
                }

                for (var sourceIndex = 0; sourceIndex < modifierSourceCount; sourceIndex++)
                {
                    graph.AddModifierSource(CreateModifierSource(sourceIndex));
                    if (++builtItems >= buildItemsPerFrame)
                    {
                        builtItems = 0;
                        lastReport = $"Building modifier sources {sourceIndex + 1}/{modifierSourceCount}";
                        yield return null;
                        if (cancelRequested)
                        {
                            yield break;
                        }
                    }
                }
            }
        }

        private void Warmup(AttributeGraph graph)
        {
            for (var i = 0; i < warmupIterations; i++)
            {
                var attributeId = attributes[i % attributes.Count];
                graph.AddBaseValue(attributeId, 0.01f);
            }
        }

        private IEnumerator WarmupCoroutine(AttributeGraph graph)
        {
            for (var i = 0; i < warmupIterations; i++)
            {
                var attributeId = attributes[i % attributes.Count];
                graph.AddBaseValue(attributeId, 0.01f);
                if (i % operationsPerFrame == operationsPerFrame - 1)
                {
                    lastReport = $"Warming up {i + 1}/{warmupIterations}";
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
                var end = Mathf.Min(i + operationsPerFrame, mutationIterations);
                for (; i < end; i++)
                {
                    graph.AddBaseValue(attributeId, 1f);
                }

                i--;
                stopwatch.Stop();
                lastReport = $"Measuring {label} {i + 1}/{mutationIterations}";
                yield return null;
                if (cancelRequested)
                {
                    break;
                }
            }

            setResult(stopwatch.Elapsed.TotalMilliseconds);
        }

        private double MeasureModifierAddRemove(AttributeGraph graph)
        {
            stopwatch.Restart();
            for (var i = 0; i < mutationIterations; i++)
            {
                var handle = graph.AddModifierSource(CreateModifierSource(modifierSourceCount + i));
                graph.RemoveModifierSource(handle);
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private IEnumerator MeasureModifierAddRemoveCoroutine(AttributeGraph graph, Action<double> setResult)
        {
            stopwatch.Reset();
            for (var i = 0; i < mutationIterations; i++)
            {
                stopwatch.Start();
                var end = Mathf.Min(i + operationsPerFrame, mutationIterations);
                for (; i < end; i++)
                {
                    var handle = graph.AddModifierSource(CreateModifierSource(modifierSourceCount + i));
                    graph.RemoveModifierSource(handle);
                }

                i--;
                stopwatch.Stop();
                lastReport = $"Measuring modifier add/remove {i + 1}/{mutationIterations}";
                yield return null;
                if (cancelRequested)
                {
                    break;
                }
            }

            setResult(stopwatch.Elapsed.TotalMilliseconds);
        }

        private ModifierSource CreateModifierSource(int sourceIndex)
        {
            var modifiers = new AttributeModifier[modifiersPerSource];
            for (var i = 0; i < modifiers.Length; i++)
            {
                var attributeIndex = Math.Abs((sourceIndex * 31 + i * 17) % attributeCount);
                var operation = i % 3 == 0 ? GEOperation.Multiply : GEOperation.Add;
                modifiers[i] = new AttributeModifier(
                    ToAttributeId(attributeIndex),
                    operation,
                    CreateModifierMagnitude(attributeIndex, operation, i));
            }

            return new ModifierSource("perf-source-" + sourceIndex, modifiers);
        }

        private IAttributeMagnitude CreateModifierMagnitude(
            int attributeIndex,
            GEOperation operation,
            int modifierIndex)
        {
            var fixedValue = operation == GEOperation.Multiply ? 1.001f : 1f + modifierIndex;
            if (!useDynamicMagnitudeModifiers || attributeIndex <= 0)
            {
                return AttributeMagnitude.Fixed(fixedValue);
            }

            var dependencyA = ToAttributeId(attributeIndex - 1);
            if (operation == GEOperation.Multiply)
            {
                return AttributeMagnitude.Add(
                    AttributeMagnitude.Fixed(1f),
                    AttributeMagnitude.Clamp(
                        AttributeMagnitude.Divide(
                            AttributeMagnitude.Attribute(dependencyA),
                            AttributeMagnitude.Fixed(100000f)),
                        AttributeMagnitude.Fixed(0f),
                        AttributeMagnitude.Fixed(0.01f)));
            }

            var dependencyB = ToAttributeId(Math.Max(0, attributeIndex - dependencyStride));
            return AttributeMagnitude.Clamp(
                AttributeMagnitude.Add(
                    AttributeMagnitude.Multiply(
                        AttributeMagnitude.Attribute(dependencyA),
                        AttributeMagnitude.Fixed(0.01f)),
                    AttributeMagnitude.Attribute(dependencyB)),
                AttributeMagnitude.Fixed(0f),
                AttributeMagnitude.Fixed(100000f));
        }

        private void AddAttribute(AttributeGraph graph, int index)
        {
            var attributeId = ToAttributeId(index);
            attributes.Add(attributeId);

            var value = new AttributeValue(
                baseValue: 100f + index % 37,
                aggregationMode: AggregationMode.Stacking,
                minValue: 0f,
                maxValue: 1000000f);

            if (index >= dependencyStride)
            {
                var dependencyA = ToAttributeId(index - 1);
                var dependencyB = ToAttributeId(index - dependencyStride);
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

        private void NormalizeSettings()
        {
            attributeCount = Mathf.Max(2, attributeCount);
            dependencyStride = Mathf.Max(1, dependencyStride);
            modifierSourceCount = Mathf.Max(0, modifierSourceCount);
            modifiersPerSource = Mathf.Max(1, modifiersPerSource);
            mutationIterations = Mathf.Max(1, mutationIterations);
            warmupIterations = Mathf.Max(0, warmupIterations);
            operationsPerFrame = Mathf.Max(1, operationsPerFrame);
            buildItemsPerFrame = Mathf.Max(1, buildItemsPerFrame);
        }

        private void FinishCanceled()
        {
            lastReport = "AttributeGraph benchmark canceled.";
            Debug.LogWarning(lastReport, this);
            isRunning = false;
        }

        private bool IsLargeBenchmark()
        {
            return attributeCount > 500 ||
                   modifierSourceCount > 100 ||
                   mutationIterations > 200;
        }

        private string BuildReport(
            double buildMs,
            double fullRecalculateMs,
            double rootMutationMs,
            double middleMutationMs,
            double leafMutationMs,
            double modifierAddRemoveMs,
            AttributeGraph graph)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PFGAS AttributeGraph Benchmark");
            sb.AppendLine($"Attributes: {attributeCount}, DependencyStride: {dependencyStride}");
            sb.AppendLine($"ModifierSources: {modifierSourceCount}, Modifiers/Source: {modifiersPerSource}");
            sb.AppendLine($"DynamicMagnitudeModifiers: {useDynamicMagnitudeModifiers}");
            sb.AppendLine($"MutationIterations: {mutationIterations}, WarmupIterations: {warmupIterations}");
            sb.AppendLine($"Batch Build: {buildMs:F3} ms");
            sb.AppendLine($"Full Recalculate: {fullRecalculateMs:F3} ms");
            sb.AppendLine($"Root Mutation Total: {rootMutationMs:F3} ms, Avg: {rootMutationMs / mutationIterations:F6} ms");
            sb.AppendLine($"Middle Mutation Total: {middleMutationMs:F3} ms, Avg: {middleMutationMs / mutationIterations:F6} ms");
            sb.AppendLine($"Leaf Mutation Total: {leafMutationMs:F3} ms, Avg: {leafMutationMs / mutationIterations:F6} ms");
            if (benchmarkModifierAddRemove)
            {
                sb.AppendLine($"Modifier Add+Remove Total: {modifierAddRemoveMs:F3} ms");
                sb.AppendLine($"Modifier Add+Remove Avg: {modifierAddRemoveMs / mutationIterations:F6} ms");
            }

            sb.AppendLine($"Sample Last Attribute: {graph.GetCurrentValue(attributes[attributes.Count - 1]):F3}");
            return sb.ToString();
        }

        private static PFAttributeId ToAttributeId(int index)
        {
            return (PFAttributeId)(9000000 + index);
        }
    }
}
