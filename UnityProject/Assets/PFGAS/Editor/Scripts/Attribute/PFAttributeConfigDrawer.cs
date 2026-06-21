using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>绘制单条 Attribute Id 定义。</summary>
    [CustomPropertyDrawer(typeof(PFAttributeConfig))]
    public sealed class PFAttributeConfigDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 3f;
        private static readonly GUIContent NameLabel = new GUIContent("属性名");
        private static readonly GUIContent CommentLabel = new GUIContent("备注");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            return property.isExpanded
                ? lineHeight * 3f + VerticalSpacing * 3f
                : lineHeight + VerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var id = property.FindPropertyRelative(nameof(PFAttributeConfig.Id));
            var name = property.FindPropertyRelative(nameof(PFAttributeConfig.Name));
            var comment = property.FindPropertyRelative(nameof(PFAttributeConfig.Comment));

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetTitle(name, id, comment, label), true);
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    line.y += line.height + VerticalSpacing;
                    EditorGUI.PropertyField(line, name, NameLabel);

                    line.y += line.height + VerticalSpacing;
                    EditorGUI.PropertyField(line, comment, CommentLabel);
                }
            }

            EditorGUI.EndProperty();
        }

        private static GUIContent GetTitle(
            SerializedProperty name,
            SerializedProperty id,
            SerializedProperty comment,
            GUIContent fallback)
        {
            if (string.IsNullOrWhiteSpace(name.stringValue))
            {
                return fallback;
            }

            var title = string.IsNullOrWhiteSpace(comment.stringValue)
                ? $"{name.stringValue} - {id.intValue}"
                : $"{name.stringValue} - {id.intValue} - {comment.stringValue}";
            return new GUIContent(title);
        }
    }

    /// <summary>绘制一个 AttributeSet。</summary>
    [CustomPropertyDrawer(typeof(PFAttributeSetConfig))]
    public sealed class PFAttributeSetConfigDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 3f;
        private static readonly GUIContent NameLabel = new GUIContent("Set 名");
        private static readonly GUIContent CommentLabel = new GUIContent("备注");
        private static readonly GUIContent AttributesLabel = new GUIContent("属性");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return lineHeight + VerticalSpacing;
            }

            var attributes = property.FindPropertyRelative(nameof(PFAttributeSetConfig.Attributes));
            return lineHeight * 3f +
                   EditorGUI.GetPropertyHeight(attributes, AttributesLabel, true) +
                   VerticalSpacing * 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var id = property.FindPropertyRelative(nameof(PFAttributeSetConfig.Id));
            var name = property.FindPropertyRelative(nameof(PFAttributeSetConfig.Name));
            var comment = property.FindPropertyRelative(nameof(PFAttributeSetConfig.Comment));
            var attributes = property.FindPropertyRelative(nameof(PFAttributeSetConfig.Attributes));

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetTitle(name, id, comment, label), true);
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    line.y += line.height + VerticalSpacing;
                    EditorGUI.PropertyField(line, name, NameLabel);

                    line.y += line.height + VerticalSpacing;
                    EditorGUI.PropertyField(line, comment, CommentLabel);

                    line.y += line.height + VerticalSpacing;
                    line.height = EditorGUI.GetPropertyHeight(attributes, AttributesLabel, true);
                    EditorGUI.PropertyField(line, attributes, AttributesLabel, true);
                }
            }

            EditorGUI.EndProperty();
        }

        private static GUIContent GetTitle(
            SerializedProperty name,
            SerializedProperty id,
            SerializedProperty comment,
            GUIContent fallback)
        {
            if (string.IsNullOrWhiteSpace(name.stringValue))
            {
                return fallback;
            }

            var title = string.IsNullOrWhiteSpace(comment.stringValue)
                ? $"{name.stringValue} - {id.intValue}"
                : $"{name.stringValue} - {id.intValue} - {comment.stringValue}";
            return new GUIContent(title);
        }
    }

    /// <summary>绘制 AttributeSet 内的单条 Attribute 初始化规则。</summary>
    [CustomPropertyDrawer(typeof(PFAttributeSetEntryConfig))]
    public sealed class PFAttributeSetEntryConfigDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 3f;
        private static readonly GUIContent AttributeLabel = new GUIContent("属性");
        private static readonly GUIContent DefaultValueLabel = new GUIContent("默认值");
        private static readonly GUIContent MinValueLabel = new GUIContent("最小值");
        private static readonly GUIContent MaxValueLabel = new GUIContent("最大值");
        private static readonly GUIContent AggregationModeLabel = new GUIContent("聚合模式");
        private static readonly GUIContent BaseValueProcessorLabel = new GUIContent("BaseValue Processor");
        private static readonly GUIContent CurrentValueProcessorLabel = new GUIContent("CurrentValue Processor");
        private static List<ProcessorOption> baseValueProcessorOptions;
        private static List<ProcessorOption> currentValueProcessorOptions;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return lineHeight + VerticalSpacing;
            }

            var baseValueProcessor = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.BaseValueProcessor));
            var currentValueProcessor = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.CurrentValueProcessor));
            return lineHeight * 6f +
                   VerticalSpacing * 6f +
                   GetProcessorFieldsHeight(baseValueProcessor) +
                   GetProcessorFieldsHeight(currentValueProcessor);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var attributeId = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.AttributeId));
            var defaultValue = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.DefaultValue));
            var aggregationMode = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.AggregationMode));
            var limitMinValue = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.LimitMinValue));
            var minValue = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.MinValue));
            var limitMaxValue = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.LimitMaxValue));
            var maxValue = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.MaxValue));
            var baseValueProcessor = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.BaseValueProcessor));
            var currentValueProcessor = property.FindPropertyRelative(nameof(PFAttributeSetEntryConfig.CurrentValueProcessor));

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetTitle(attributeId, label), true);
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    line.y += line.height + VerticalSpacing;
                    EditorGUI.PropertyField(line, attributeId, AttributeLabel);

                    line.y += line.height + VerticalSpacing;
                    DrawValueLine(line, defaultValue, limitMinValue, minValue, limitMaxValue, maxValue);

                    line.y += line.height + VerticalSpacing;
                    EditorGUI.PropertyField(line, aggregationMode, AggregationModeLabel);

                    line.y += line.height + VerticalSpacing;
                    DrawProcessor(
                        ref line,
                        baseValueProcessor,
                        BaseValueProcessorLabel,
                        typeof(PFAttributeBaseValueProcessorConfig),
                        typeof(PFDefaultAttributeBaseValueProcessorConfig),
                        ref baseValueProcessorOptions);

                    line.y += line.height + VerticalSpacing;
                    DrawProcessor(
                        ref line,
                        currentValueProcessor,
                        CurrentValueProcessorLabel,
                        typeof(PFAttributeCurrentValueProcessorConfig),
                        typeof(PFDefaultAttributeCurrentValueProcessorConfig),
                        ref currentValueProcessorOptions);
                }
            }

            EditorGUI.EndProperty();
        }

        private static GUIContent GetTitle(SerializedProperty attributeId, GUIContent fallback)
        {
            return attributeId.intValue < 0
                ? fallback
                : new GUIContent($"Attribute - {attributeId.intValue}");
        }

        private static void DrawValueLine(
            Rect rect,
            SerializedProperty defaultValue,
            SerializedProperty limitMinValue,
            SerializedProperty minValue,
            SerializedProperty limitMaxValue,
            SerializedProperty maxValue)
        {
            const float spacing = 8f;
            const float toggleWidth = 72f;
            var defaultWidth = Mathf.Min(270f, rect.width * 0.32f);
            var valueWidth = (rect.width - defaultWidth - toggleWidth * 2f - spacing * 4f) * 0.5f;
            valueWidth = Mathf.Max(80f, valueWidth);

            var defaultRect = new Rect(rect.x, rect.y, defaultWidth, rect.height);
            var minToggleRect = new Rect(defaultRect.xMax + spacing, rect.y, toggleWidth, rect.height);
            var minValueRect = new Rect(minToggleRect.xMax + spacing * 0.5f, rect.y, valueWidth, rect.height);
            var maxToggleRect = new Rect(minValueRect.xMax + spacing, rect.y, toggleWidth, rect.height);
            var maxValueRect = new Rect(maxToggleRect.xMax + spacing * 0.5f, rect.y, rect.xMax - maxToggleRect.xMax - spacing * 0.5f, rect.height);

            EditorGUI.PropertyField(defaultRect, defaultValue, DefaultValueLabel);
            limitMinValue.boolValue = EditorGUI.ToggleLeft(minToggleRect, MinValueLabel, limitMinValue.boolValue);
            using (new EditorGUI.DisabledScope(!limitMinValue.boolValue))
            {
                EditorGUI.PropertyField(minValueRect, minValue, GUIContent.none);
            }

            limitMaxValue.boolValue = EditorGUI.ToggleLeft(maxToggleRect, MaxValueLabel, limitMaxValue.boolValue);
            using (new EditorGUI.DisabledScope(!limitMaxValue.boolValue))
            {
                EditorGUI.PropertyField(maxValueRect, maxValue, GUIContent.none);
            }
        }

        private static void DrawProcessor(
            ref Rect line,
            SerializedProperty processor,
            GUIContent label,
            Type processorBaseType,
            Type defaultProcessorType,
            ref List<ProcessorOption> optionsCache)
        {
            EnsureProcessor(processor, defaultProcessorType);

            var options = GetProcessorOptions(processorBaseType, defaultProcessorType, ref optionsCache);
            var selectedIndex = FindProcessorIndex(processor.managedReferenceValue?.GetType(), options);
            var labels = new GUIContent[options.Count];
            for (var i = 0; i < options.Count; i++)
            {
                labels[i] = options[i].Label;
            }

            var nextIndex = EditorGUI.Popup(line, label, selectedIndex, labels);
            if (nextIndex != selectedIndex)
            {
                processor.managedReferenceValue = Activator.CreateInstance(options[nextIndex].Type);
            }

            DrawProcessorFields(ref line, processor);
        }

        private static void DrawProcessorFields(ref Rect line, SerializedProperty processor)
        {
            var iterator = processor.Copy();
            var end = iterator.GetEndProperty();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                var height = EditorGUI.GetPropertyHeight(iterator, true);
                line.y += line.height + VerticalSpacing;
                line.height = height;
                EditorGUI.PropertyField(line, iterator, true);
            }

            line.height = EditorGUIUtility.singleLineHeight;
        }

        private static float GetProcessorFieldsHeight(SerializedProperty processor)
        {
            if (processor == null || processor.managedReferenceValue == null)
            {
                return 0f;
            }

            var height = 0f;
            var iterator = processor.Copy();
            var end = iterator.GetEndProperty();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator, true) + VerticalSpacing;
            }

            return height;
        }

        private static void EnsureProcessor(SerializedProperty processor, Type defaultProcessorType)
        {
            if (processor.managedReferenceValue != null)
            {
                return;
            }

            processor.managedReferenceValue = Activator.CreateInstance(defaultProcessorType);
        }

        private static int FindProcessorIndex(Type type, IReadOnlyList<ProcessorOption> options)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].Type == type)
                {
                    return i;
                }
            }

            return 0;
        }

        private static List<ProcessorOption> GetProcessorOptions(
            Type processorBaseType,
            Type defaultProcessorType,
            ref List<ProcessorOption> optionsCache)
        {
            if (optionsCache != null)
            {
                return optionsCache;
            }

            optionsCache = new List<ProcessorOption>();
            var types = TypeCache.GetTypesDerivedFrom(processorBaseType);
            foreach (var type in types)
            {
                if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                var instance = (PFAttributeValueProcessorConfig)Activator.CreateInstance(type);
                optionsCache.Add(new ProcessorOption(type, new GUIContent(instance.DisplayName)));
            }

            optionsCache.Sort((a, b) => CompareProcessorOptions(a, b, defaultProcessorType));
            return optionsCache;
        }

        private static int CompareProcessorOptions(
            ProcessorOption a,
            ProcessorOption b,
            Type defaultProcessorType)
        {
            if (a.Type == defaultProcessorType)
            {
                return b.Type == defaultProcessorType ? 0 : -1;
            }

            if (b.Type == defaultProcessorType)
            {
                return 1;
            }

            return string.Compare(a.Label.text, b.Label.text, StringComparison.Ordinal);
        }

        private readonly struct ProcessorOption
        {
            public ProcessorOption(Type type, GUIContent label)
            {
                Type = type;
                Label = label;
            }

            public readonly Type Type;
            public readonly GUIContent Label;
        }
    }
}
