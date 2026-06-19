using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>集中放置 AttributeGraph 的通用私有校验和读取工具。</summary>
    public sealed partial class AttributeGraph
    {
        private bool IsBatching => batchDepth > 0;

        private void EnsureCanMutate()
        {
            if (isPublishingChanges)
            {
                GASGuard.ThrowInvalidOperation(
                    "AttributeGraph cannot be mutated during attribute change event publication.");
            }
        }

        private AttributeNode GetNode(PFAttributeId attributeId)
        {
            if (!nodes.TryGetValue(attributeId, out var node))
            {
                GASGuard.ThrowKeyNotFound($"Attribute '{attributeId}' is not registered.");
            }

            return node;
        }

        private void ValidateModifierDependencies(AttributeModifier modifier)
        {
            var dependencies = modifier.Magnitude.Dependencies;
            for (var i = 0; i < dependencies.Count; i++)
            {
                var dependencyId = dependencies[i];
                if (!nodes.ContainsKey(dependencyId))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"Modifier target '{modifier.AttributeId}' depends on missing attribute '{dependencyId}'.");
                }
            }
        }
    }
}
