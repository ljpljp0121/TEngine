using System;
using System.Collections.Generic;
using PFGAS.Runtime;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>Attribute 编辑器中可生成运行时 IAttributeEvaluator 的配置基类。</summary>
    [Serializable]
    public abstract class PFAttributeEvaluatorConfig
    {
        public abstract string DisplayName { get; }

        public abstract bool TryBuildEvaluatorExpression(
            PFAttributeEvaluatorCodeContext context,
            out string expression,
            out string error);

        protected static bool TryResolveDependency(
            PFAttributeEvaluatorCodeContext context,
            int attributeId,
            out PFAttributeGenerationAttributeInfo attribute,
            out string error)
        {
            if (!context.TryResolveAttribute(attributeId, out attribute, out error))
            {
                return false;
            }

            if (attribute.Id == context.Owner.Id)
            {
                error = "Evaluator dependency cannot reference the owner attribute.";
                return false;
            }

            return true;
        }
    }

    /// <summary>Evaluator 生成代码时用于解析属性名到 AttributeId 的上下文。</summary>
    public sealed class PFAttributeEvaluatorCodeContext
    {
        private readonly IReadOnlyDictionary<int, PFAttributeGenerationAttributeInfo> attributesById;

        public PFAttributeEvaluatorCodeContext(
            PFAttributeGenerationAttributeInfo owner,
            IReadOnlyDictionary<int, PFAttributeGenerationAttributeInfo> attributesById)
        {
            Owner = owner;
            this.attributesById = attributesById;
        }

        public PFAttributeGenerationAttributeInfo Owner { get; }

        public bool TryResolveAttribute(
            int attributeId,
            out PFAttributeGenerationAttributeInfo attribute,
            out string error)
        {
            if (attributeId < 0)
            {
                attribute = null;
                error = "Required attribute is empty.";
                return false;
            }

            if (!attributesById.TryGetValue(attributeId, out attribute))
            {
                error = $"Required attribute id '{attributeId}' does not exist.";
                return false;
            }

            error = null;
            return true;
        }

        public string GetAttributeIdExpression(PFAttributeGenerationAttributeInfo attribute)
        {
            return "PFAttributeId." + attribute.EnumName;
        }
    }

    /// <summary>代码生成阶段对单个属性的只读描述。</summary>
    public sealed class PFAttributeGenerationAttributeInfo
    {
        public PFAttributeGenerationAttributeInfo(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }
        public int Id { get; }
        public string EnumName => Name;
        public string RuleName => EnumName;
    }

    /// <summary>把字符串字段绘制为当前 Attribute 配置中的 Attribute 下拉框。</summary>
    public sealed class PFAttributeReferenceAttribute : PropertyAttribute
    {
    }
}
