using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>维护 ModifierSource 句柄、Modifier 索引以及 Magnitude 依赖边。</summary>
    public sealed partial class AttributeGraph
    {
        /// <summary>保存 ModifierSource 并把其中的 Modifier 加入按属性查询的索引。</summary>
        private ModifierSourceHandle AddModifierSourceToStore(ModifierSource source)
        {
            var handle = new ModifierSourceHandle(nextModifierSourceHandle++);
            modifierSources.Add(handle, source);
            AddSourceToModifierIndex(source);
            return handle;
        }

        /// <summary>忽略返回值地移除 source，主要用于失败回滚。</summary>
        private void RemoveModifierSourceFromStore(ModifierSourceHandle handle)
        {
            if (modifierSources.Remove(handle, out var source))
            {
                RemoveSourceFromModifierIndex(source);
            }
        }

        /// <summary>检查属性是否仍有活跃 Modifier。</summary>
        private bool HasModifiers(PFAttributeId attributeId)
        {
            return modifiersByAttribute.ContainsKey(attributeId);
        }

        /// <summary>获取作用在指定属性上的 Modifier 列表。</summary>
        private IReadOnlyList<AttributeModifier> GetModifiers(PFAttributeId attributeId)
        {
            return modifiersByAttribute.TryGetValue(attributeId, out var modifiers)
                ? modifiers
                : System.Array.Empty<AttributeModifier>();
        }

        private void AddSourceToModifierIndex(ModifierSource source)
        {
            foreach (var modifier in source.Modifiers)
            {
                if (!modifiersByAttribute.TryGetValue(modifier.AttributeId, out var modifiers))
                {
                    modifiers = new List<AttributeModifier>();
                    modifiersByAttribute.Add(modifier.AttributeId, modifiers);
                }

                modifiers.Add(modifier);
            }
        }

        private void RemoveSourceFromModifierIndex(ModifierSource source)
        {
            for (var i = 0; i < source.Modifiers.Count; i++)
            {
                var modifier = source.Modifiers[i];
                if (!modifiersByAttribute.TryGetValue(modifier.AttributeId, out var modifiers))
                {
                    continue;
                }

                RemoveModifier(modifiers, modifier);
                if (modifiers.Count == 0)
                {
                    modifiersByAttribute.Remove(modifier.AttributeId);
                }
            }
        }

        private static void RemoveModifier(List<AttributeModifier> modifiers, AttributeModifier modifier)
        {
            for (var i = modifiers.Count - 1; i >= 0; i--)
            {
                var current = modifiers[i];
                if (current.AttributeId.Equals(modifier.AttributeId) &&
                    current.Operation == modifier.Operation &&
                    Equals(current.Magnitude, modifier.Magnitude))
                {
                    modifiers.RemoveAt(i);
                    return;
                }
            }
        }

        private void AddModifierDependencyEdges(ModifierSource source)
        {
            for (var i = 0; i < source.Modifiers.Count; i++)
            {
                var modifier = source.Modifiers[i];
                var dependencies = modifier.Magnitude.Dependencies;
                for (var dependencyIndex = 0; dependencyIndex < dependencies.Count; dependencyIndex++)
                {
                    AddDependencyReference(modifier.AttributeId, dependencies[dependencyIndex]);
                }
            }
        }

        private void RemoveModifierDependencyEdges(ModifierSource source)
        {
            for (var i = 0; i < source.Modifiers.Count; i++)
            {
                var modifier = source.Modifiers[i];
                if (!nodes.ContainsKey(modifier.AttributeId))
                {
                    continue;
                }

                var dependencies = modifier.Magnitude.Dependencies;
                for (var dependencyIndex = 0; dependencyIndex < dependencies.Count; dependencyIndex++)
                {
                    RemoveDependencyReference(modifier.AttributeId, dependencies[dependencyIndex]);
                }
            }
        }
    }
}
