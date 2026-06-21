using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>负责 dirty 收集、拓扑顺序重算和 Attribute raw value 聚合。</summary>
    public sealed partial class AttributeGraph
    {
        private void RecalculateAllInternal()
        {
            RecalculateNodes(GetCachedTopologicalOrder(), HasChangeListeners);
        }

        private void RecalculateDirtySet()
        {
            RecalculateDirtySet(HasChangeListeners);
        }

        private void RecalculateDirtySet(bool trackChanges)
        {
            GetCachedTopologicalOrder();
            reusableSelectedNodes.Clear();
            foreach (var attributeId in reusableDirtySet)
            {
                if (nodes.TryGetValue(attributeId, out var node))
                {
                    reusableSelectedNodes.Add(node);
                }
            }

            reusableSelectedNodes.Sort(CompareTopologyIndex);
            RecalculateNodes(reusableSelectedNodes, trackChanges);
            reusableSelectedNodes.Clear();
            reusableDirtySet.Clear();
        }

        /// <summary>从属性向下游收集所有需要重算的节点，保证增量重算不漏掉依赖者。</summary>
        private void CollectDirtyInto(PFAttributeId attributeId, HashSet<PFAttributeId> dirty)
        {
            reusableDirtyStack.Push(attributeId);

            while (reusableDirtyStack.Count > 0)
            {
                var current = reusableDirtyStack.Pop();
                if (!dirty.Add(current) || !nodes.TryGetValue(current, out var node))
                {
                    continue;
                }

                foreach (var dependentId in node.Dependents)
                {
                    reusableDirtyStack.Push(dependentId);
                }
            }
        }

        private void CollectModifierTargetsInto(ModifierSource source, HashSet<PFAttributeId> dirty)
        {
            for (var i = 0; i < source.Modifiers.Count; i++)
            {
                CollectDirtyInto(source.Modifiers[i].AttributeId, dirty);
            }
        }

        private void CommitPartialMutation(MutationTransaction transaction, PFAttributeId dirtyRoot)
        {
            if (IsBatching)
            {
                CollectDirtyInto(dirtyRoot, reusableDirtySet);
                pendingPartialRecalculate = true;
                transaction.Commit();
                return;
            }

            reusableDirtySet.Clear();
            CollectDirtyInto(dirtyRoot, reusableDirtySet);
            transaction.SnapshotValues(reusableDirtySet);
            RecalculateDirtySet();
            transaction.Commit();
            PublishAttributeChanges();
        }

        private void CommitPartialMutation(MutationTransaction transaction, ModifierSource source)
        {
            if (IsBatching)
            {
                CollectModifierTargetsInto(source, reusableDirtySet);
                pendingPartialRecalculate = true;
                transaction.Commit();
                return;
            }

            reusableDirtySet.Clear();
            CollectModifierTargetsInto(source, reusableDirtySet);
            transaction.SnapshotValues(reusableDirtySet);
            RecalculateDirtySet();
            transaction.Commit();
            PublishAttributeChanges();
        }

        private void CommitAddedAttribute(MutationTransaction transaction, PFAttributeId addedId)
        {
            if (IsBatching)
            {
                CollectDirtyInto(addedId, reusableDirtySet);
                pendingPartialRecalculate = true;
                transaction.Commit();
                return;
            }

            transaction.SnapshotAllValues();
            RecalculateAllInternal();
            transaction.Commit();
            PublishAttributeChanges();
        }

        private void CommitAddedAttributes(
            MutationTransaction transaction,
            IReadOnlyList<PFAttributeId> addedIds)
        {
            if (IsBatching)
            {
                for (var i = 0; i < addedIds.Count; i++)
                {
                    CollectDirtyInto(addedIds[i], reusableDirtySet);
                }

                pendingPartialRecalculate = true;
                transaction.Commit();
                return;
            }

            transaction.SnapshotAllValues();
            RecalculateAllInternal();
            transaction.Commit();
            PublishAttributeChanges();
        }

        private void CommitFullMutation(MutationTransaction transaction, bool snapshotValues)
        {
            if (IsBatching)
            {
                pendingFullRecalculate = true;
                transaction.Commit();
                return;
            }

            if (snapshotValues)
            {
                transaction.SnapshotAllValues();
            }

            RecalculateAllInternal();
            transaction.Commit();
            PublishAttributeChanges();
        }

        /// <summary>按拓扑顺序重算节点，并在值发生有效变化时记录事件快照。</summary>
        private void RecalculateNodes(IReadOnlyList<AttributeNode> orderedNodes, bool trackChanges)
        {
            foreach (var node in orderedNodes)
            {
                var value = node.Value;
                var oldBaseValue = value.BaseValue;
                value.SetBaseValue(ProcessBaseValue(node, value.BaseValue));
                if (PFGASHelper.HasMeaningfulChange(oldBaseValue, value.BaseValue))
                {
                    TrackOriginalValue(node, trackChanges);
                    node.Value = value;
                }

                var rawValue = CalculateRawValue(node);
                var finalValue = node.CurrentValueProcessor.Process(context, node.Id, rawValue);
                if (!PFGASHelper.IsFinite(finalValue))
                {
                    GASGuard.ThrowInvalidOperation($"Attribute '{node.Id}' processed to a non-finite current value.");
                }

                value = node.Value;
                var oldCurrentValue = value.CurrentValue;
                value.SetCurrentValue(finalValue);
                if (PFGASHelper.HasMeaningfulChange(oldCurrentValue, value.CurrentValue))
                {
                    TrackOriginalValue(node, trackChanges);
                    node.Value = value;
                }
            }
        }

        private float ProcessBaseValue(AttributeNode node, float proposedBaseValue)
        {
            var processedValue = node.BaseValueProcessor.Process(context, node.Id, proposedBaseValue);
            if (!PFGASHelper.IsFinite(processedValue))
            {
                GASGuard.ThrowInvalidOperation($"Attribute '{node.Id}' processed to a non-finite base value.");
            }

            return processedValue;
        }

        /// <summary>根据属性聚合模式计算进入 CurrentValue processor 前的 raw value。</summary>
        private float CalculateRawValue(AttributeNode node)
        {
            switch (node.Value.CalculateMode)
            {
                case AggregationMode.Stacking:
                    return CalculateStackingRawValue(node);
                case AggregationMode.MinValueOnly:
                    return CalculateMinOrMaxRawValue(node, true);
                case AggregationMode.MaxValueOnly:
                    return CalculateMinOrMaxRawValue(node, false);
            }

            return CalculateStackingRawValue(node);
        }

        /// <summary>Stacking 模式：Add 累加、Multiply 连乘，Override 存在时覆盖最终 raw value。</summary>
        private float CalculateStackingRawValue(AttributeNode node)
        {
            var add = 0f;
            var multiply = 1f;
            var hasOverride = false;
            var overrideValue = 0f;
            var modifiers = GetModifiers(node.Id);

            for (var i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                switch (modifier.Operation)
                {
                    case GEOperation.Add:
                        add += modifier.EvaluateMagnitude(context);
                        break;
                    case GEOperation.Multiply:
                        multiply *= modifier.EvaluateMagnitude(context);
                        break;
                    case GEOperation.Override:
                        hasOverride = true;
                        overrideValue = modifier.EvaluateMagnitude(context);
                        break;
                }
            }

            return hasOverride ? overrideValue : (node.Value.BaseValue + add) * multiply;
        }

        /// <summary>Min/Max 模式：每个 Modifier 独立作用在 BaseValue 上，再选择最小或最大候选值。</summary>
        private float CalculateMinOrMaxRawValue(AttributeNode node, bool chooseMin)
        {
            var selected = node.Value.BaseValue;
            var modifiers = GetModifiers(node.Id);
            for (var i = 0; i < modifiers.Count; i++)
            {
                var candidate = ApplySingleModifier(node.Value.BaseValue, modifiers[i], context);
                selected = chooseMin
                    ? Math.Min(selected, candidate)
                    : Math.Max(selected, candidate);
            }

            return selected;
        }

        private float ApplySingleModifier(float baseValue, AttributeModifier modifier, AttributeGraphContext graphContext)
        {
            var magnitude = modifier.EvaluateMagnitude(graphContext);
            switch (modifier.Operation)
            {
                case GEOperation.Add:
                    return baseValue + magnitude;
                case GEOperation.Multiply:
                    return baseValue * magnitude;
                case GEOperation.Override:
                    return magnitude;
            }

            return baseValue + magnitude;
        }
    }
}
