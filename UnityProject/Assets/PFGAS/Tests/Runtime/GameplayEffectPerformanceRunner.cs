using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PFGAS.Runtime.Tests
{
    public sealed class GameplayEffectPerformanceRunner : MonoBehaviour
    {
        [SerializeField] private bool runOnStart;
        [SerializeField] private bool drawRuntimePanel = true;
        [SerializeField] private int targetCount = 64;
        [SerializeField] private int instantApplyIterations = 2000;
        [SerializeField] private int durationEffectsPerTarget = 4;
        [SerializeField] private int dynamicSourceMutationIterations = 64;
        [SerializeField] private int periodicTickIterations = 32;
        [SerializeField] private string lastReport;

        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly Stopwatch stopwatch = new Stopwatch();

        public string LastReport => lastReport;

        private void Start()
        {
            if (runOnStart)
            {
                RunBenchmark();
            }
        }

        private void OnDestroy()
        {
            CleanupCreatedObjects();
        }

        [ContextMenu("Run GameplayEffect Benchmark")]
        public void RunBenchmark()
        {
            NormalizeSettings();
            CleanupCreatedObjects();

            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(true);

            var instantApplyMs = MeasureInstantApply();
            var durationApplyMs = MeasureDurationApply(out var durationApplyCount, out var durationRemoveAllMs);
            var dynamicEventStormMs = MeasureDynamicSourceEventStorm(out var eventStormRebuildCount);
            var dynamicSteadyFrameMs = MeasureDynamicSourceSteadyFrames(out var steadyFrameRebuildCount);
            var periodicTickMs = MeasurePeriodicTicks();

            GC.Collect();
            var memoryAfter = GC.GetTotalMemory(true);

            lastReport = BuildReport(
                instantApplyMs,
                durationApplyMs,
                durationApplyCount,
                durationRemoveAllMs,
                dynamicEventStormMs,
                eventStormRebuildCount,
                dynamicSteadyFrameMs,
                steadyFrameRebuildCount,
                periodicTickMs,
                memoryAfter - memoryBefore);

            Debug.Log(lastReport, this);
            CleanupCreatedObjects();
        }

        private void OnGUI()
        {
            if (!drawRuntimePanel || string.IsNullOrEmpty(lastReport))
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 260f, 820f, 260f), GUI.skin.box);
            GUILayout.Label(lastReport);
            GUILayout.EndArea();
        }

        private double MeasureInstantApply()
        {
            CleanupCreatedObjects();
            var source = CreateUnit("InstantSource");
            var targets = CreateTargets("InstantTarget", targetCount);
            var effect = new GameplayEffect(
                "PerfInstantDamage",
                GameplayEffectLifetime.Instant,
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Instant,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-1f),
                        GameplayEffectCapturePolicy.SnapshotOnApply),
                });

            stopwatch.Restart();
            for (var i = 0; i < instantApplyIterations; i++)
            {
                var result = targets[i % targets.Count].Effects.ApplyToSelf(effect, source);
                if (result.Failed)
                {
                    throw new InvalidOperationException(result.Failure.ToString());
                }
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private double MeasureDurationApply(out int applyCount, out double removeAllMs)
        {
            CleanupCreatedObjects();
            var source = CreateUnit("DurationSource");
            var targets = CreateTargets("DurationTarget", targetCount);
            var effect = new GameplayEffect(
                "PerfDurationBuff",
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

            applyCount = targets.Count * durationEffectsPerTarget;
            stopwatch.Restart();
            for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                var target = targets[targetIndex];
                for (var effectIndex = 0; effectIndex < durationEffectsPerTarget; effectIndex++)
                {
                    var result = target.Effects.ApplyToSelf(effect, source);
                    if (result.Failed)
                    {
                        throw new InvalidOperationException(result.Failure.ToString());
                    }
                }
            }

            stopwatch.Stop();
            var applyMs = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            for (var i = 0; i < targets.Count; i++)
            {
                targets[i].Effects.RemoveAll();
            }

            stopwatch.Stop();
            removeAllMs = stopwatch.Elapsed.TotalMilliseconds;
            return applyMs;
        }

        private double MeasureDynamicSourceEventStorm(out int rebuildCount)
        {
            CleanupCreatedObjects();
            var source = CreateUnit("DynamicSource");
            var targets = CreateTargets("DynamicTarget", targetCount);
            var effect = new GameplayEffect(
                "PerfDynamicSourceBuff",
                GameplayEffectLifetime.ForDuration(1000f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.01f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            for (var i = 0; i < targets.Count; i++)
            {
                var result = targets[i].Effects.ApplyToSelf(effect, source);
                if (result.Failed)
                {
                    throw new InvalidOperationException(result.Failure.ToString());
                }
            }

            stopwatch.Restart();
            for (var i = 0; i < dynamicSourceMutationIterations; i++)
            {
                source.Attributes.AddBaseValue(PFAttributeId.MaxHP, 1f);
            }

            for (var i = 0; i < targets.Count; i++)
            {
                targets[i].Effects.Tick(0f);
            }

            stopwatch.Stop();
            rebuildCount = targets.Count;
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private double MeasureDynamicSourceSteadyFrames(out int rebuildCount)
        {
            CleanupCreatedObjects();
            var source = CreateUnit("DynamicSteadySource");
            var targets = CreateTargets("DynamicSteadyTarget", targetCount);
            var effect = new GameplayEffect(
                "PerfDynamicSteadySourceBuff",
                GameplayEffectLifetime.ForDuration(1000f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Ongoing,
                        PFAttributeId.MaxHP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.SourceAttribute(PFAttributeId.MaxHP, 0.01f),
                        GameplayEffectCapturePolicy.DynamicWhileActive),
                });

            for (var i = 0; i < targets.Count; i++)
            {
                var result = targets[i].Effects.ApplyToSelf(effect, source);
                if (result.Failed)
                {
                    throw new InvalidOperationException(result.Failure.ToString());
                }
            }

            stopwatch.Restart();
            for (var mutationIndex = 0; mutationIndex < dynamicSourceMutationIterations; mutationIndex++)
            {
                source.Attributes.AddBaseValue(PFAttributeId.MaxHP, 1f);
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    targets[targetIndex].Effects.Tick(0f);
                }
            }

            stopwatch.Stop();
            rebuildCount = targets.Count * dynamicSourceMutationIterations;
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private double MeasurePeriodicTicks()
        {
            CleanupCreatedObjects();
            var source = CreateUnit("PeriodicSource");
            var targets = CreateTargets("PeriodicTarget", targetCount);
            var effect = new GameplayEffect(
                "PerfPeriodicDamage",
                GameplayEffectLifetime.ForDuration(periodicTickIterations + 10f, period: 1f),
                new[]
                {
                    new GameplayEffectModifierSpec(
                        GameplayEffectModifierPhase.Periodic,
                        PFAttributeId.HP,
                        GEOperation.Add,
                        GameplayEffectMagnitudeSpec.Fixed(-1f),
                        GameplayEffectCapturePolicy.ReevaluateOnPeriod),
                });

            for (var i = 0; i < targets.Count; i++)
            {
                var result = targets[i].Effects.ApplyToSelf(effect, source);
                if (result.Failed)
                {
                    throw new InvalidOperationException(result.Failure.ToString());
                }
            }

            stopwatch.Restart();
            for (var tick = 0; tick < periodicTickIterations; tick++)
            {
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    targets[targetIndex].Effects.Tick(1f);
                }
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
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

        private List<CombatUnit> CreateTargets(string prefix, int count)
        {
            var targets = new List<CombatUnit>(count);
            for (var i = 0; i < count; i++)
            {
                targets.Add(CreateUnit(prefix + i));
            }

            return targets;
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

        private void NormalizeSettings()
        {
            targetCount = Mathf.Max(1, targetCount);
            instantApplyIterations = Mathf.Max(1, instantApplyIterations);
            durationEffectsPerTarget = Mathf.Max(1, durationEffectsPerTarget);
            dynamicSourceMutationIterations = Mathf.Max(1, dynamicSourceMutationIterations);
            periodicTickIterations = Mathf.Max(1, periodicTickIterations);
        }

        private string BuildReport(
            double instantApplyMs,
            double durationApplyMs,
            int durationApplyCount,
            double durationRemoveAllMs,
            double dynamicEventStormMs,
            int eventStormRebuildCount,
            double dynamicSteadyFrameMs,
            int steadyFrameRebuildCount,
            double periodicTickMs,
            long managedMemoryDelta)
        {
            var periodicTickCount = targetCount * periodicTickIterations;
            var sb = new StringBuilder();
            sb.AppendLine("PFGAS GameplayEffect Benchmark");
            sb.AppendLine($"Targets: {targetCount}");
            sb.AppendLine($"Instant Applies: {instantApplyIterations}, Total: {instantApplyMs:F3} ms, Avg: {instantApplyMs / instantApplyIterations:F6} ms");
            sb.AppendLine($"Duration Applies: {durationApplyCount}, Total: {durationApplyMs:F3} ms, Avg: {durationApplyMs / durationApplyCount:F6} ms");
            sb.AppendLine($"Duration RemoveAll Targets: {targetCount}, Total: {durationRemoveAllMs:F3} ms, Avg/Target: {durationRemoveAllMs / targetCount:F6} ms");
            sb.AppendLine($"Dynamic Event Storm Mutations: {dynamicSourceMutationIterations}, Coalesced Rebuilds: {eventStormRebuildCount}");
            sb.AppendLine($"Dynamic Event Storm Total: {dynamicEventStormMs:F3} ms, Avg/Rebuild: {dynamicEventStormMs / eventStormRebuildCount:F6} ms");
            sb.AppendLine($"Dynamic Steady Frames: {dynamicSourceMutationIterations}, Total Rebuilds: {steadyFrameRebuildCount}");
            sb.AppendLine($"Dynamic Steady Total: {dynamicSteadyFrameMs:F3} ms, Avg/Rebuild: {dynamicSteadyFrameMs / steadyFrameRebuildCount:F6} ms");
            sb.AppendLine($"Periodic Target Ticks: {periodicTickCount}, Total: {periodicTickMs:F3} ms, Avg/TargetTick: {periodicTickMs / periodicTickCount:F6} ms");
            sb.AppendLine($"Managed Memory Delta After GC: {managedMemoryDelta} bytes");
            return sb.ToString();
        }
    }
}
