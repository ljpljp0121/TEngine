using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>把属性 Id 绘制为 Attribute 配置资产里的下拉框。</summary>
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

            var config = property.serializedObject.targetObject as PFAttributeConfigAsset;
            var options = property.name == nameof(PFAttributeSetEntryConfig.AttributeId)
                ? CollectGlobalAttributeOptions(config)
                : CollectAttributeSetDependencyOptions(config, property);

            if (options.Count <= 1)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.TextField(position, label, "No Attributes");
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

        private static List<AttributeOption> CollectGlobalAttributeOptions(PFAttributeConfigAsset config)
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

                AddAttributeOption(options, attribute);
            }

            return options;
        }

        private static List<AttributeOption> CollectAttributeSetDependencyOptions(
            PFAttributeConfigAsset config,
            SerializedProperty property)
        {
            var options = new List<AttributeOption>
            {
                new AttributeOption(-1, "<None>")
            };

            if (config == null)
            {
                return options;
            }

            var ownerAttributeId = GetOwnerSetEntryAttributeId(property);
            var setAttributes = GetOwnerSetAttributes(property);
            if (setAttributes == null || !setAttributes.isArray)
            {
                return options;
            }

            for (var i = 0; i < setAttributes.arraySize; i++)
            {
                var entry = setAttributes.GetArrayElementAtIndex(i);
                var attributeId = entry.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.AttributeId));
                if (attributeId == null || attributeId.intValue < 0 || attributeId.intValue == ownerAttributeId)
                {
                    continue;
                }

                var attribute = FindAttributeConfig(config, attributeId.intValue);
                if (attribute == null)
                {
                    options.Add(new AttributeOption(attributeId.intValue, $"Missing - {attributeId.intValue}"));
                    continue;
                }

                AddAttributeOption(options, attribute);
            }

            return options;
        }

        private static void AddAttributeOption(List<AttributeOption> options, PFAttributeConfig attribute)
        {
            var attributeName = NormalizeName(attribute.Name);
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                return;
            }

            options.Add(new AttributeOption(attribute.Id, $"{attributeName} - {attribute.Id}"));
        }

        private static PFAttributeConfig FindAttributeConfig(PFAttributeConfigAsset config, int attributeId)
        {
            if (config.Attributes == null)
            {
                return null;
            }

            for (var i = 0; i < config.Attributes.Length; i++)
            {
                var attribute = config.Attributes[i];
                if (attribute != null && attribute.Id == attributeId)
                {
                    return attribute;
                }
            }

            return null;
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

        private static int GetOwnerSetEntryAttributeId(SerializedProperty property)
        {
            var owner = GetOwnerSetEntry(property);
            return owner == null
                ? -1
                : owner.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.AttributeId))?.intValue ?? -1;
        }

        private static SerializedProperty GetOwnerSetEntry(SerializedProperty property)
        {
            var processorIndex = property.propertyPath.IndexOf(".BaseValueProcessor", System.StringComparison.Ordinal);
            if (processorIndex < 0)
            {
                processorIndex = property.propertyPath.IndexOf(".CurrentValueProcessor", System.StringComparison.Ordinal);
                if (processorIndex < 0)
                {
                    return null;
                }
            }

            var ownerPath = property.propertyPath.Substring(0, processorIndex);
            return property.serializedObject.FindProperty(ownerPath);
        }

        private static SerializedProperty GetOwnerSetAttributes(SerializedProperty property)
        {
            var owner = GetOwnerSetEntry(property);
            if (owner == null)
            {
                return null;
            }

            var marker = "." + nameof(PFAttributeSetConfig.Attributes) + ".Array.data[";
            var markerIndex = owner.propertyPath.LastIndexOf(marker, System.StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            var setAttributesPath = owner.propertyPath.Substring(0, markerIndex) +
                                    "." + nameof(PFAttributeSetConfig.Attributes);
            return property.serializedObject.FindProperty(setAttributesPath);
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
