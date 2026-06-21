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
        public PFAttributeConfig[] Attributes = Array.Empty<PFAttributeConfig>();

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

        public PFAttributeConfig()
        {
            Name = "Default";
            Comment = "";
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
