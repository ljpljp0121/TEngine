using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>处理属性注册、processor 依赖替换以及失败后的状态回滚。</summary>
    public sealed partial class AttributeGraph
    {
        private void AddAttributeRule(AttributeRule rule, AttributeValue value)
        {
            ValidateAttributeRules(new[] { rule });

            using var transaction = BeginMutationTransaction();
            nodes.Add(rule.Id, new AttributeNode(rule.Id, value));
            transaction.RecordAddedAttribute(rule.Id);
            MarkTopologyDirty();
            ApplyProcessors(rule.Id, GetNode(rule.Id), rule, transaction);
            RebuildTopology();
            CommitAddedAttribute(transaction, rule.Id);
        }

        /// <summary>批量规则先整体校验，避免只注册一半时留下不可恢复依赖。</summary>
        private void ValidateAttributeRules(IReadOnlyList<AttributeRule> rules)
        {
            var newRules = new Dictionary<PFAttributeId, AttributeRule>();
            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (nodes.ContainsKey(rule.Id))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"Attribute '{rule.Id}' has already been added.");
                }

                if (newRules.ContainsKey(rule.Id))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"Attribute rule '{rule.Id}' is duplicated.");
                }

                newRules.Add(rule.Id, rule);
            }

            foreach (var pair in newRules)
            {
                var dependencies = pair.Value.RequiredAttributes;
                for (var i = 0; i < dependencies.Count; i++)
                {
                    var dependencyId = dependencies[i];
                    if (!nodes.ContainsKey(dependencyId) && !newRules.ContainsKey(dependencyId))
                    {
                        GASGuard.ThrowInvalidOperation(
                            $"Attribute rule '{pair.Key}' depends on missing attribute '{dependencyId}'.");
                    }
                }
            }

            var visitStates = new Dictionary<PFAttributeId, int>();
            foreach (var pair in newRules)
            {
                VisitRuleDependencies(pair.Key, newRules, visitStates);
            }
        }

        private static void VisitRuleDependencies(
            PFAttributeId attributeId,
            Dictionary<PFAttributeId, AttributeRule> rules,
            Dictionary<PFAttributeId, int> visitStates)
        {
            if (!rules.TryGetValue(attributeId, out var rule))
            {
                return;
            }

            if (visitStates.TryGetValue(attributeId, out var state))
            {
                if (state == 1)
                {
                    GASGuard.ThrowInvalidOperation(
                        "Attribute rules contain a dependency cycle.");
                }

                return;
            }

            visitStates.Add(attributeId, 1);
            for (var i = 0; i < rule.RequiredAttributes.Count; i++)
            {
                VisitRuleDependencies(rule.RequiredAttributes[i], rules, visitStates);
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
            AttributeRule rule,
            MutationTransaction transaction)
        {
            var baseValueDependencies = ValidateProcessorDependencies(
                attributeId,
                rule.BaseValueProcessor.Dependencies);
            var currentValueDependencies = ValidateProcessorDependencies(
                attributeId,
                rule.CurrentValueProcessor.Dependencies);
            ApplyProcessors(
                attributeId,
                node,
                rule.BaseValueProcessor,
                baseValueDependencies,
                rule.CurrentValueProcessor,
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
