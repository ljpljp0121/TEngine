using System;
using System.Collections.Generic;
using PFGAS.Runtime;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>Attribute 编辑器中可生成运行时属性值处理器的配置基类。</summary>
    [Serializable]
    public abstract class PFAttributeValueProcessorConfig
    {
        public abstract string DisplayName { get; }

        protected static bool TryResolveDependency(
            PFAttributeProcessorCodeContext context,
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
                error = "Processor dependency cannot reference the owner attribute.";
                return false;
            }

            return true;
        }
    }

    /// <summary>可生成运行时 IAttributeBaseValueProcessor 的配置基类。</summary>
    [Serializable]
    public abstract class PFAttributeBaseValueProcessorConfig : PFAttributeValueProcessorConfig
    {
        public abstract bool TryBuildProcessorExpression(
            PFAttributeProcessorCodeContext context,
            out string expression,
            out string error);
    }

    /// <summary>可生成运行时 IAttributeCurrentValueProcessor 的配置基类。</summary>
    [Serializable]
    public abstract class PFAttributeCurrentValueProcessorConfig : PFAttributeValueProcessorConfig
    {
        public abstract bool TryBuildProcessorExpression(
            PFAttributeProcessorCodeContext context,
            out string expression,
            out string error);
    }

    /// <summary>处理器生成代码时用于解析属性名到 AttributeId 的上下文。</summary>
    public sealed class PFAttributeProcessorCodeContext
    {
        private readonly IReadOnlyDictionary<int, PFAttributeGenerationAttributeInfo> attributesById;

        public PFAttributeProcessorCodeContext(
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

    /// <summary>把整数字段绘制为当前 Attribute 配置中的 Attribute 下拉框。</summary>
    public sealed class PFAttributeReferenceAttribute : PropertyAttribute
    {
    }
}
