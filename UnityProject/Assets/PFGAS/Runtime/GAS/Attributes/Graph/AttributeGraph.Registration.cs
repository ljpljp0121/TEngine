using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>处理属性注册、processor 依赖替换以及失败后的状态回滚。</summary>
    public sealed partial class AttributeGraph
    {
        private void AddAttributeSetEntries(IReadOnlyList<AttributeSetEntry> entries)
        {
            ValidateAttributeSetEntries(entries);

            using var transaction = BeginMutationTransaction();
            var addedIds = new List<PFAttributeId>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                nodes.Add(entry.Id, new AttributeNode(entry.Id, entry.CreateValue()));
                transaction.RecordAddedAttribute(entry.Id);
                addedIds.Add(entry.Id);
            }

            MarkTopologyDirty();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                ApplyProcessors(entry.Id, GetNode(entry.Id), entry, transaction);
            }

            RebuildTopology();
            CommitAddedAttributes(transaction, addedIds);
        }

        /// <summary>AttributeSet 条目先整体校验，避免只注册一半时留下不可恢复依赖。</summary>
        private void ValidateAttributeSetEntries(IReadOnlyList<AttributeSetEntry> entries)
        {
            var newEntries = new Dictionary<PFAttributeId, AttributeSetEntry>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (nodes.ContainsKey(entry.Id))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"Attribute '{entry.Id}' has already been added.");
                }

                if (newEntries.ContainsKey(entry.Id))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"AttributeSet entry '{entry.Id}' is duplicated.");
                }

                newEntries.Add(entry.Id, entry);
            }

            foreach (var pair in newEntries)
            {
                var dependencies = pair.Value.RequiredAttributes;
                for (var i = 0; i < dependencies.Count; i++)
                {
                    var dependencyId = dependencies[i];
                    if (!nodes.ContainsKey(dependencyId) && !newEntries.ContainsKey(dependencyId))
                    {
                        GASGuard.ThrowInvalidOperation(
                            $"AttributeSet entry '{pair.Key}' depends on missing attribute '{dependencyId}'.");
                    }
                }
            }

            var visitStates = new Dictionary<PFAttributeId, int>();
            foreach (var pair in newEntries)
            {
                VisitEntryDependencies(pair.Key, newEntries, visitStates);
            }
        }

        private static void VisitEntryDependencies(
            PFAttributeId attributeId,
            Dictionary<PFAttributeId, AttributeSetEntry> entries,
            Dictionary<PFAttributeId, int> visitStates)
        {
            if (!entries.TryGetValue(attributeId, out var entry))
            {
                return;
            }

            if (visitStates.TryGetValue(attributeId, out var state))
            {
                if (state == 1)
                {
                    GASGuard.ThrowInvalidOperation(
                        "AttributeSet entries contain a dependency cycle.");
                }

                return;
            }

            visitStates.Add(attributeId, 1);
            for (var i = 0; i < entry.RequiredAttributes.Count; i++)
            {
                VisitEntryDependencies(entry.RequiredAttributes[i], entries, visitStates);
            }

            visitStates[attributeId] = 2;
        }

        private PFAttributeId[] ValidateProcessorDependencies(
            PFAttributeId attributeId,
            IReadOnlyList<PFAttributeId> dependencies)
        {
            if (dependencies.Count == 0)
            {
                return Array.Empty<PFAttributeId>();
            }

            var unique = new HashSet<PFAttributeId>();
            var result = new List<PFAttributeId>(dependencies.Count);
            for (var i = 0; i < dependencies.Count; i++)
            {
                var dependencyId = dependencies[i];
                if (!nodes.ContainsKey(dependencyId))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"Attribute '{attributeId}' depends on missing attribute '{dependencyId}'.");
                }

                if (unique.Add(dependencyId))
                {
                    result.Add(dependencyId);
                }
            }

            return result.ToArray();
        }

        private void ApplyProcessors(
            PFAttributeId attributeId,
            AttributeNode node,
            AttributeSetEntry entry,
            MutationTransaction transaction)
        {
            var baseValueDependencies = ValidateProcessorDependencies(
                attributeId,
                entry.BaseValueProcessor.Dependencies);
            var currentValueDependencies = ValidateProcessorDependencies(
                attributeId,
                entry.CurrentValueProcessor.Dependencies);
            ApplyProcessors(
                attributeId,
                node,
                entry.BaseValueProcessor,
                baseValueDependencies,
                entry.CurrentValueProcessor,
                currentValueDependencies,
                transaction);
        }

        private void ApplyProcessors(
            PFAttributeId attributeId,
            AttributeNode node,
            IAttributeBaseValueProcessor baseValueProcessor,
            IReadOnlyList<PFAttributeId> baseValueDependencies,
            IAttributeCurrentValueProcessor currentValueProcessor,
            IReadOnlyList<PFAttributeId> currentValueDependencies,
            MutationTransaction transaction)
        {
            transaction.RecordProcessors(attributeId, node);
            ApplyBaseValueProcessor(attributeId, node, baseValueProcessor, baseValueDependencies);
            ApplyCurrentValueProcessor(attributeId, node, currentValueProcessor, currentValueDependencies);
        }

        private void ApplyBaseValueProcessor(
            PFAttributeId attributeId,
            AttributeNode node,
            IAttributeBaseValueProcessor processor,
            IReadOnlyList<PFAttributeId> dependencies)
        {
            ReplaceBaseValueProcessorDependencies(attributeId, node, dependencies, skipMissingDependencies: false);
            node.BaseValueProcessor = processor;
        }

        private void ApplyBaseValueProcessor(
            PFAttributeId attributeId,
            AttributeNode node,
            IAttributeBaseValueProcessor processor,
            IReadOnlyList<PFAttributeId> dependencies,
            MutationTransaction transaction)
        {
            transaction.RecordProcessors(attributeId, node);
            ApplyBaseValueProcessor(attributeId, node, processor, dependencies);
        }

        private void ApplyCurrentValueProcessor(
            PFAttributeId attributeId,
            AttributeNode node,
            IAttributeCurrentValueProcessor processor,
            IReadOnlyList<PFAttributeId> dependencies)
        {
            ReplaceCurrentValueProcessorDependencies(attributeId, node, dependencies, skipMissingDependencies: false);
            node.CurrentValueProcessor = processor;
        }

        private void ApplyCurrentValueProcessor(
            PFAttributeId attributeId,
            AttributeNode node,
            IAttributeCurrentValueProcessor processor,
            IReadOnlyList<PFAttributeId> dependencies,
            MutationTransaction transaction)
        {
            transaction.RecordProcessors(attributeId, node);
            ApplyCurrentValueProcessor(attributeId, node, processor, dependencies);
        }

        private void RestoreProcessors(
            PFAttributeId attributeId,
            AttributeNode node,
            IAttributeBaseValueProcessor baseValueProcessor,
            IReadOnlyList<PFAttributeId> baseValueDependencies,
            IAttributeCurrentValueProcessor currentValueProcessor,
            IReadOnlyList<PFAttributeId> currentValueDependencies)
        {
            ReplaceBaseValueProcessorDependencies(
                attributeId,
                node,
                baseValueDependencies,
                skipMissingDependencies: true);
            ReplaceCurrentValueProcessorDependencies(
                attributeId,
                node,
                currentValueDependencies,
                skipMissingDependencies: true);
            node.BaseValueProcessor = baseValueProcessor;
            node.CurrentValueProcessor = currentValueProcessor;
        }
    }
}
