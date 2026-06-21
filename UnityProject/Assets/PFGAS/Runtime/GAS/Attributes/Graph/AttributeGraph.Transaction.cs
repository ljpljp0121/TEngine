using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    public sealed partial class AttributeGraph
    {
        private MutationTransaction mutationTransaction;

        private MutationTransaction BeginMutationTransaction()
        {
            mutationTransaction ??= new MutationTransaction(this);
            return mutationTransaction.Begin();
        }

        /// <summary>保存一次运行时数值重算前的图状态；失败时恢复，成功时提交。</summary>
        private sealed class MutationTransaction : IDisposable
        {
            private readonly AttributeGraph graph;
            private readonly List<ValueSnapshot> valueSnapshots = new();
            private readonly List<Action> rollbackActions = new();
            private readonly List<PFAttributeId> dirtySetSnapshot = new();
            private readonly List<PFAttributeId> dirtyStackSnapshot = new();
            private readonly List<AttributeNode> selectedNodesSnapshot = new();
            private readonly List<AttributeChange> reusableChangesSnapshot = new();
            private readonly List<TrackedChangeSnapshot> trackedChangeSnapshots = new();

            private bool active;
            private bool committed;
            private bool pendingFullRecalculateSnapshot;
            private bool pendingPartialRecalculateSnapshot;

            public MutationTransaction(AttributeGraph graph)
            {
                this.graph = graph;
            }

            public MutationTransaction Begin()
            {
                if (active)
                {
                    GASGuard.ThrowInvalidOperation("AttributeGraph mutation transaction is already active.");
                }

                active = true;
                committed = false;
                CaptureGraphState();
                return this;
            }

            public void SnapshotValues(IEnumerable<PFAttributeId> attributeIds)
            {
                foreach (var attributeId in attributeIds)
                {
                    SnapshotValue(attributeId);
                }
            }

            public void SnapshotAllValues()
            {
                foreach (var pair in graph.nodes)
                {
                    SnapshotValue(pair.Key);
                }
            }

            public void RecordAddedAttribute(PFAttributeId attributeId)
            {
                rollbackActions.Add(() =>
                {
                    graph.DetachNodeFromDependencies(attributeId);
                    graph.nodes.Remove(attributeId);
                    graph.RemoveTrackedChange(attributeId);
                    graph.MarkTopologyDirty();
                });
            }

            public void RecordRemovedAttribute(PFAttributeId attributeId, AttributeNode node)
            {
                rollbackActions.Add(() =>
                {
                    if (graph.nodes.ContainsKey(attributeId))
                    {
                        return;
                    }

                    graph.nodes.Add(attributeId, node);
                    foreach (var dependencyId in node.Dependencies)
                    {
                        if (graph.nodes.TryGetValue(dependencyId, out var dependency))
                        {
                            dependency.Dependents.Add(attributeId);
                        }
                    }

                    graph.MarkTopologyDirty();
                });
            }

            public void RecordProcessors(PFAttributeId attributeId, AttributeNode node)
            {
                var baseValueProcessor = node.BaseValueProcessor;
                var baseValueDependencies = new PFAttributeId[node.BaseValueProcessorDependencies.Count];
                node.BaseValueProcessorDependencies.CopyTo(baseValueDependencies);
                var currentValueProcessor = node.CurrentValueProcessor;
                var currentValueDependencies = new PFAttributeId[node.CurrentValueProcessorDependencies.Count];
                node.CurrentValueProcessorDependencies.CopyTo(currentValueDependencies);
                rollbackActions.Add(() =>
                    graph.RestoreProcessors(
                        attributeId,
                        node,
                        baseValueProcessor,
                        baseValueDependencies,
                        currentValueProcessor,
                        currentValueDependencies));
            }

            public void RecordAddedModifierSource(ModifierSourceHandle handle, ModifierSource source)
            {
                rollbackActions.Add(() =>
                {
                    graph.RemoveModifierDependencyEdges(source);
                    graph.RemoveModifierSourceFromStore(handle);
                    graph.MarkTopologyDirty();
                });
            }

            public void RecordRemovedModifierSource(ModifierSourceHandle handle, ModifierSource source)
            {
                var modifierIndexSnapshots = CaptureModifierIndexes(source);
                rollbackActions.Add(() =>
                {
                    if (!graph.modifierSources.ContainsKey(handle))
                    {
                        graph.modifierSources.Add(handle, source);
                    }

                    for (var i = 0; i < modifierIndexSnapshots.Count; i++)
                    {
                        modifierIndexSnapshots[i].Restore(graph);
                    }

                    graph.AddModifierDependencyEdges(source);
                    graph.MarkTopologyDirty();
                });
            }

            public void Commit()
            {
                committed = true;
            }

            public void Dispose()
            {
                try
                {
                    if (active && !committed)
                    {
                        Rollback();
                    }
                }
                finally
                {
                    active = false;
                    ClearSnapshots();
                }
            }

            private void CaptureGraphState()
            {
                pendingFullRecalculateSnapshot = graph.pendingFullRecalculate;
                pendingPartialRecalculateSnapshot = graph.pendingPartialRecalculate;

                dirtySetSnapshot.Clear();
                foreach (var attributeId in graph.reusableDirtySet)
                {
                    dirtySetSnapshot.Add(attributeId);
                }

                dirtyStackSnapshot.Clear();
                foreach (var attributeId in graph.reusableDirtyStack)
                {
                    dirtyStackSnapshot.Add(attributeId);
                }

                selectedNodesSnapshot.Clear();
                selectedNodesSnapshot.AddRange(graph.reusableSelectedNodes);

                reusableChangesSnapshot.Clear();
                reusableChangesSnapshot.AddRange(graph.reusableChanges);

                trackedChangeSnapshots.Clear();
                foreach (var pair in graph.originalChangedValues)
                {
                    trackedChangeSnapshots.Add(new TrackedChangeSnapshot(pair.Key, pair.Value));
                }
            }

            private void SnapshotValue(PFAttributeId attributeId)
            {
                if (!graph.nodes.TryGetValue(attributeId, out var node) ||
                    HasValueSnapshot(attributeId))
                {
                    return;
                }

                valueSnapshots.Add(new ValueSnapshot(attributeId, node.Value));
            }

            private bool HasValueSnapshot(PFAttributeId attributeId)
            {
                for (var i = 0; i < valueSnapshots.Count; i++)
                {
                    if (valueSnapshots[i].AttributeId.Equals(attributeId))
                    {
                        return true;
                    }
                }

                return false;
            }

            private List<ModifierIndexSnapshot> CaptureModifierIndexes(ModifierSource source)
            {
                var snapshots = new List<ModifierIndexSnapshot>();
                var uniqueAttributeIds = new HashSet<PFAttributeId>();
                for (var i = 0; i < source.Modifiers.Count; i++)
                {
                    var attributeId = source.Modifiers[i].AttributeId;
                    if (!uniqueAttributeIds.Add(attributeId))
                    {
                        continue;
                    }

                    if (graph.modifiersByAttribute.TryGetValue(attributeId, out var modifiers))
                    {
                        snapshots.Add(new ModifierIndexSnapshot(attributeId, modifiers.ToArray()));
                    }
                    else
                    {
                        snapshots.Add(new ModifierIndexSnapshot(attributeId, Array.Empty<AttributeModifier>()));
                    }
                }

                return snapshots;
            }

            private void Rollback()
            {
                for (var i = rollbackActions.Count - 1; i >= 0; i--)
                {
                    rollbackActions[i]();
                }

                for (var i = 0; i < valueSnapshots.Count; i++)
                {
                    var snapshot = valueSnapshots[i];
                    if (graph.nodes.TryGetValue(snapshot.AttributeId, out var node))
                    {
                        node.Value = snapshot.Value;
                    }
                }

                graph.pendingFullRecalculate = pendingFullRecalculateSnapshot;
                graph.pendingPartialRecalculate = pendingPartialRecalculateSnapshot;

                graph.reusableDirtySet.Clear();
                for (var i = 0; i < dirtySetSnapshot.Count; i++)
                {
                    graph.reusableDirtySet.Add(dirtySetSnapshot[i]);
                }

                graph.reusableSelectedNodes.Clear();
                graph.reusableSelectedNodes.AddRange(selectedNodesSnapshot);

                graph.reusableDirtyStack.Clear();
                for (var i = dirtyStackSnapshot.Count - 1; i >= 0; i--)
                {
                    graph.reusableDirtyStack.Push(dirtyStackSnapshot[i]);
                }

                graph.reusableChanges.Clear();
                graph.reusableChanges.AddRange(reusableChangesSnapshot);

                graph.originalChangedValues.Clear();
                for (var i = 0; i < trackedChangeSnapshots.Count; i++)
                {
                    var snapshot = trackedChangeSnapshots[i];
                    graph.originalChangedValues.Add(snapshot.AttributeId, snapshot.Value);
                }
            }

            private void ClearSnapshots()
            {
                valueSnapshots.Clear();
                rollbackActions.Clear();
                dirtySetSnapshot.Clear();
                dirtyStackSnapshot.Clear();
                selectedNodesSnapshot.Clear();
                reusableChangesSnapshot.Clear();
                trackedChangeSnapshots.Clear();
            }

            private readonly struct ValueSnapshot
            {
                public ValueSnapshot(PFAttributeId attributeId, AttributeValue value)
                {
                    AttributeId = attributeId;
                    Value = value;
                }

                public readonly PFAttributeId AttributeId;
                public readonly AttributeValue Value;
            }

            private readonly struct TrackedChangeSnapshot
            {
                public TrackedChangeSnapshot(PFAttributeId attributeId, AttributeValue value)
                {
                    AttributeId = attributeId;
                    Value = value;
                }

                public readonly PFAttributeId AttributeId;
                public readonly AttributeValue Value;
            }

            private readonly struct ModifierIndexSnapshot
            {
                public ModifierIndexSnapshot(PFAttributeId attributeId, AttributeModifier[] modifiers)
                {
                    AttributeId = attributeId;
                    Modifiers = modifiers;
                }

                public void Restore(AttributeGraph graph)
                {
                    if (Modifiers.Length == 0)
                    {
                        graph.modifiersByAttribute.Remove(AttributeId);
                        return;
                    }

                    graph.modifiersByAttribute[AttributeId] = new List<AttributeModifier>(Modifiers);
                }

                private readonly PFAttributeId AttributeId;
                private readonly AttributeModifier[] Modifiers;
            }
        }
    }
}
