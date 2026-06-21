using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>AttributeGraph 的对外调用入口，集中展示外部系统可以使用的能力。</summary>
    public sealed partial class AttributeGraph
    {
        public event Action<AttributeChange> AttributeChanged;

        public event Action<AttributeChange[]> AttributesChanged;

        public int Count => nodes.Count;

        /// <summary>创建批量修改作用域；最外层作用域结束时统一重算并发布事件。</summary>
        public IDisposable BatchUpdate()
        {
            EnsureCanMutate();
            BeginBatchUpdate();
            return new BatchScope(this);
        }

        /// <summary>把当前注册的 AttributeId 复制到调用方提供的列表中。</summary>
        public void GetAttributeIds(List<PFAttributeId> results)
        {
            results.Clear();
            foreach (var attributeId in nodes.Keys)
            {
                results.Add(attributeId);
            }
        }

        /// <summary>注册单个属性，并立即绑定 BaseValue/CurrentValue processor 和依赖边。</summary>
        public void AddAttribute(
            PFAttributeId attributeId,
            AttributeValue value,
            IAttributeBaseValueProcessor baseValueProcessor = null,
            IAttributeCurrentValueProcessor currentValueProcessor = null)
        {
            EnsureCanMutate();
            if (nodes.ContainsKey(attributeId))
            {
                GASGuard.ThrowInvalidOperation($"Attribute '{attributeId}' has already been added.");
            }

            using var transaction = BeginMutationTransaction();
            nodes.Add(attributeId, new AttributeNode(attributeId, value));
            transaction.RecordAddedAttribute(attributeId);
            MarkTopologyDirty();
            baseValueProcessor ??= DefaultAttributeBaseValueProcessor.Instance;
            currentValueProcessor ??= DefaultAttributeCurrentValueProcessor.Instance;
            var baseValueDependencies = ValidateProcessorDependencies(attributeId, baseValueProcessor.Dependencies);
            var currentValueDependencies = ValidateProcessorDependencies(attributeId, currentValueProcessor.Dependencies);
            var node = GetNode(attributeId);
            ApplyProcessors(
                attributeId,
                node,
                baseValueProcessor,
                baseValueDependencies,
                currentValueProcessor,
                currentValueDependencies,
                transaction);
            RebuildTopology();
            CommitPartialMutation(transaction, attributeId);
        }

        /// <summary>注册一个 AttributeSet；Set 内的处理器依赖必须由同一个 Set 提供。</summary>
        public void AddAttributeSet(AttributeSet attributeSet)
        {
            EnsureCanMutate();
            if (attributeSet == null)
            {
                GASGuard.ThrowArgument("AttributeSet cannot be null.", nameof(attributeSet));
            }

            AddAttributeSetEntries(attributeSet.Entries);
        }

        /// <summary>移除无依赖者且无活跃 Modifier 的属性。</summary>
        public bool RemoveAttribute(PFAttributeId attributeId)
        {
            EnsureCanMutate();
            if (!nodes.TryGetValue(attributeId, out var node))
            {
                return false;
            }

            if (node.Dependents.Count > 0)
            {
                GASGuard.ThrowInvalidOperation(
                    $"Attribute '{attributeId}' is still used by dependent attributes.");
            }

            if (HasModifiers(attributeId))
            {
                GASGuard.ThrowInvalidOperation(
                    $"Attribute '{attributeId}' still has active modifiers.");
            }

            using var transaction = BeginMutationTransaction();
            transaction.SnapshotAllValues();
            transaction.RecordRemovedAttribute(attributeId, node);
            DetachNodeFromDependencies(attributeId);
            nodes.Remove(attributeId);
            RemoveTrackedChange(attributeId);
            MarkTopologyDirty();
            CommitFullMutation(transaction, snapshotValues: false);

            return true;
        }

        /// <summary>尝试读取属性完整值快照。</summary>
        public bool TryGetValue(PFAttributeId attributeId, out AttributeValue value)
        {
            if (nodes.TryGetValue(attributeId, out var node))
            {
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>读取属性完整值；属性不存在时抛出异常。</summary>
        public AttributeValue GetValue(PFAttributeId attributeId)
        {
            return GetNode(attributeId).Value;
        }

        /// <summary>读取属性基础值。</summary>
        public float GetBaseValue(PFAttributeId attributeId)
        {
            return GetNode(attributeId).Value.BaseValue;
        }

        /// <summary>读取属性当前最终值。</summary>
        public float GetCurrentValue(PFAttributeId attributeId)
        {
            return GetNode(attributeId).Value.CurrentValue;
        }

        /// <summary>设置基础值并重算该属性及其下游依赖者。</summary>
        public bool SetBaseValue(PFAttributeId attributeId, float value)
        {
            EnsureCanMutate();
            var node = GetNode(attributeId);
            var attributeValue = node.Value;
            var oldBaseValue = attributeValue.BaseValue;
            attributeValue.SetBaseValue(value);
            attributeValue.SetBaseValue(ProcessBaseValue(node, attributeValue.BaseValue));

            if (PFGASHelper.IsNearlyEqual(oldBaseValue, attributeValue.BaseValue))
            {
                return false;
            }

            if (IsBatching)
            {
                TrackOriginalValue(node, HasChangeListeners);
                node.Value = attributeValue;
                CollectDirtyInto(attributeId, reusableDirtySet);
                pendingPartialRecalculate = true;
            }
            else
            {
                using var transaction = BeginMutationTransaction();
                CollectDirtyInto(attributeId, reusableDirtySet);
                transaction.SnapshotValues(reusableDirtySet);

                TrackOriginalValue(node, HasChangeListeners);
                node.Value = attributeValue;
                RecalculateDirtySet();
                transaction.Commit();
                PublishAttributeChanges();
            }

            return true;
        }

        /// <summary>在现有基础值上叠加变化量。</summary>
        public bool AddBaseValue(PFAttributeId attributeId, float delta)
        {
            return SetBaseValue(attributeId, GetBaseValue(attributeId) + delta);
        }

        /// <summary>批量应用会落到 BaseValue 的 Modifier，并执行 BaseValue processor。</summary>
        public AttributeChange[] ApplyBaseModifiers(IEnumerable<AttributeModifier> modifiers)
        {
            EnsureCanMutate();
            if (IsBatching)
            {
                GASGuard.ThrowInvalidOperation(
                    "ApplyBaseModifiers cannot run inside AttributeGraph batch updates.");
            }

            var modifierList = new List<AttributeModifier>();
            foreach (var modifier in modifiers)
            {
                if (!nodes.ContainsKey(modifier.AttributeId))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"Modifier target '{modifier.AttributeId}' is not registered.");
                }

                ValidateModifierDependencies(modifier);
                modifierList.Add(modifier);
            }

            if (modifierList.Count == 0)
            {
                return Array.Empty<AttributeChange>();
            }

            using var transaction = BeginMutationTransaction();
            reusableDirtySet.Clear();
            for (var i = 0; i < modifierList.Count; i++)
            {
                CollectDirtyInto(modifierList[i].AttributeId, reusableDirtySet);
            }

            // Snapshot before applying modifiers so a later magnitude/evaluator failure restores original values.
            transaction.SnapshotValues(reusableDirtySet);

            foreach (var modifier in modifierList)
            {
                var node = GetNode(modifier.AttributeId);
                TrackOriginalValue(node, true);

                var attributeValue = node.Value;
                var newBaseValue = ApplySingleModifier(attributeValue.BaseValue, modifier, context);
                attributeValue.SetBaseValue(newBaseValue);
                attributeValue.SetBaseValue(ProcessBaseValue(node, attributeValue.BaseValue));
                node.Value = attributeValue;
            }

            RecalculateDirtySet(true);
            transaction.Commit();

            var changes = CollectAttributeChanges();
            PublishAttributeChanges(changes);
            return changes;
        }

        public void SetBaseValueProcessor(PFAttributeId attributeId, IAttributeBaseValueProcessor processor)
        {
            EnsureCanMutate();
            processor ??= DefaultAttributeBaseValueProcessor.Instance;
            var dependencies = ValidateProcessorDependencies(attributeId, processor.Dependencies);

            var node = GetNode(attributeId);
            using var transaction = BeginMutationTransaction();
            ApplyBaseValueProcessor(attributeId, node, processor, dependencies, transaction);
            RebuildTopology();
            CommitPartialMutation(transaction, attributeId);
        }

        public void SetCurrentValueProcessor(PFAttributeId attributeId, IAttributeCurrentValueProcessor processor)
        {
            EnsureCanMutate();
            processor ??= DefaultAttributeCurrentValueProcessor.Instance;
            var dependencies = ValidateProcessorDependencies(attributeId, processor.Dependencies);

            var node = GetNode(attributeId);
            using var transaction = BeginMutationTransaction();
            ApplyCurrentValueProcessor(attributeId, node, processor, dependencies, transaction);
            RebuildTopology();
            CommitPartialMutation(transaction, attributeId);
        }

        /// <summary>添加一组 Modifier，并把 Magnitude 依赖纳入拓扑关系。</summary>
        public ModifierSourceHandle AddModifierSource(ModifierSource source)
        {
            EnsureCanMutate();
            for (var i = 0; i < source.Modifiers.Count; i++)
            {
                if (!nodes.ContainsKey(source.Modifiers[i].AttributeId))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"Modifier target '{source.Modifiers[i].AttributeId}' is not registered.");
                }

                ValidateModifierDependencies(source.Modifiers[i]);
            }

            using var transaction = BeginMutationTransaction();
            var handle = AddModifierSourceToStore(source);
            transaction.RecordAddedModifierSource(handle, source);
            AddModifierDependencyEdges(source);
            RebuildTopology();
            CommitPartialMutation(transaction, source);

            return handle;
        }

        /// <summary>移除指定 ModifierSource，并清理它贡献的 Modifier 和依赖边。</summary>
        public bool RemoveModifierSource(ModifierSourceHandle handle)
        {
            EnsureCanMutate();
            if (!modifierSources.TryGetValue(handle, out var source))
            {
                return false;
            }

            using var transaction = BeginMutationTransaction();
            transaction.RecordRemovedModifierSource(handle, source);
            RemoveModifierSourceFromStore(handle);
            RemoveModifierDependencyEdges(source);
            RebuildTopology();
            CommitPartialMutation(transaction, source);

            return true;
        }

        /// <summary>强制按当前拓扑顺序全量重算所有属性。</summary>
        public void RecalculateAll()
        {
            EnsureCanMutate();
            if (IsBatching)
            {
                pendingFullRecalculate = true;
                return;
            }

            using (var transaction = BeginMutationTransaction())
            {
                transaction.SnapshotAllValues();
                RecalculateAllInternal();
                transaction.Commit();
            }

            PublishAttributeChanges();
        }

        private sealed class BatchScope : IDisposable
        {
            private readonly AttributeGraph graph;

            public BatchScope(AttributeGraph graph)
            {
                this.graph = graph;
            }

            public void Dispose()
            {
                graph.EndBatchUpdate();
            }
        }
    }
}
