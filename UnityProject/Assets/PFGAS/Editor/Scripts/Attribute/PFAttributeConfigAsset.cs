using System;
using PFGAS.Runtime;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>属性编辑器的全局配置资产，保存所有属性定义数据。</summary>
    [CreateAssetMenu(fileName = "PFAttributeConfig", menuName = "PFGAS/AttributeConfig")]
    public class PFAttributeConfigAsset : ScriptableObject
    {
        [HideInInspector]
        public int MaxId = 0;
        [HideInInspector]
        public int MaxSetId = 0;
        public PFAttributeConfig[] Attributes = Array.Empty<PFAttributeConfig>();
        public PFAttributeSetConfig[] AttributeSets = Array.Empty<PFAttributeSetConfig>();

        private void OnValidate()
        {
            if (PFAttributeCodeGenerator.EnsureStableIds(this, out var changed, out _)
                && changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
    }

    /// <summary>单条属性定义的编辑器序列化数据。</summary>
    [Serializable]
    public class PFAttributeConfig
    {
        [HideInInspector]
        public int Id;
        public string Name;
        public string Comment;

        public PFAttributeConfig()
        {
            Name = "Default";
            Comment = "";
        }
    }

    /// <summary>可以整体挂到 CombatUnit 上的一组 Attribute 初始化规则。</summary>
    [Serializable]
    public class PFAttributeSetConfig
    {
        [HideInInspector]
        public int Id;
        public string Name;
        public string Comment;
        public PFAttributeSetEntryConfig[] Attributes = Array.Empty<PFAttributeSetEntryConfig>();

        public PFAttributeSetConfig()
        {
            Name = "Default";
            Comment = "";
        }
    }

    /// <summary>AttributeSet 中某个 Attribute 的默认值、范围和后处理器。</summary>
    [Serializable]
    public class PFAttributeSetEntryConfig
    {
        [PFAttributeReference]
        public int AttributeId = -1;
        public float DefaultValue;
        public AggregationMode AggregationMode;
        public bool LimitMinValue;
        public float MinValue;
        public bool LimitMaxValue;
        public float MaxValue;
        [SerializeReference]
        public PFAttributeBaseValueProcessorConfig BaseValueProcessor;
        [SerializeReference]
        public PFAttributeCurrentValueProcessorConfig CurrentValueProcessor;

        public PFAttributeSetEntryConfig()
        {
            DefaultValue = 0;
            AggregationMode = AggregationMode.Stacking;
            LimitMinValue = false;
            MinValue = 0;
            LimitMaxValue = false;
            MaxValue = 999999;
            BaseValueProcessor = new PFDefaultAttributeBaseValueProcessorConfig();
            CurrentValueProcessor = new PFDefaultAttributeCurrentValueProcessorConfig();
        }
    }
}
