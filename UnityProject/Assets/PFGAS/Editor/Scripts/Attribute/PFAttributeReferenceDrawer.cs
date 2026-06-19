using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>把属性名字符串绘制成当前 Attribute 配置资产里的 Attribute 下拉框。</summary>
    [CustomPropertyDrawer(typeof(PFAttributeReferenceAttribute))]
    public sealed class PFAttributeReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var ownerAttributeId = GetOwnerAttributeId(property);
            var options = CollectAttributeOptions(
                property.serializedObject.targetObject as PFAttributeConfigAsset,
                ownerAttributeId);
            if (options.Count <= 1)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.TextField(position, label, "No Other Attributes");
                }

                return;
            }

            var selectedIndex = FindSelectedIndex(options, property.intValue);
            var labels = new GUIContent[options.Count];
            for (var i = 0; i < options.Count; i++)
            {
                labels[i] = new GUIContent(options[i].Label);
            }

            EditorGUI.BeginChangeCheck();
            var nextIndex = EditorGUI.Popup(position, label, selectedIndex, labels);
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = options[nextIndex].AttributeId;
            }
        }

        private static List<AttributeOption> CollectAttributeOptions(
            PFAttributeConfigAsset config,
            int excludedAttributeId)
        {
            var options = new List<AttributeOption>
            {
                new AttributeOption(-1, "<None>")
            };

            if (config == null || config.Attributes == null)
            {
                return options;
            }

            for (var attributeIndex = 0; attributeIndex < config.Attributes.Length; attributeIndex++)
            {
                var attribute = config.Attributes[attributeIndex];
                if (attribute == null)
                {
                    continue;
                }

                var attributeName = NormalizeName(attribute.Name);
                if (string.IsNullOrWhiteSpace(attributeName))
                {
                    continue;
                }

                if (attribute.Id == excludedAttributeId)
                {
                    continue;
                }

                options.Add(new AttributeOption(attribute.Id, $"{attributeName} - {attribute.Id}"));
            }

            return options;
        }

        private static int FindSelectedIndex(IReadOnlyList<AttributeOption> options, int value)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].AttributeId == value)
                {
                    return i;
                }
            }

            return 0;
        }

        private static int GetOwnerAttributeId(SerializedProperty property)
        {
            var evaluatorIndex = property.propertyPath.IndexOf(".Evaluator", System.StringComparison.Ordinal);
            if (evaluatorIndex < 0)
            {
                return -1;
            }

            var ownerPath = property.propertyPath.Substring(0, evaluatorIndex);
            var owner = property.serializedObject.FindProperty(ownerPath);
            return owner == null
                ? -1
                : owner.FindPropertyRelative(nameof(PFAttributeConfig.Id))?.intValue ?? -1;
        }

        private static string NormalizeName(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        private readonly struct AttributeOption
        {
            public AttributeOption(int attributeId, string label)
            {
                AttributeId = attributeId;
                Label = label;
            }

            public readonly int AttributeId;
            public readonly string Label;
        }
    }
}
