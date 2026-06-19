using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>绘制单条 Attribute 定义配置。</summary>
    [CustomPropertyDrawer(typeof(PFAttributeConfig))]
    public sealed class PFAttributeConfigDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 3f;
        private static readonly GUIContent NameLabel = new GUIContent("\u5c5e\u6027\u540d");
        private static readonly GUIContent CommentLabel = new GUIContent("\u5907\u6ce8");
        private static readonly GUIContent DefaultValueLabel = new GUIContent("\u9ed8\u8ba4\u503c");
        private static readonly GUIContent MinValueLabel = new GUIContent("\u6700\u5c0f\u503c");
        private static readonly GUIContent MaxValueLabel = new GUIContent("\u6700\u5927\u503c");
        private static readonly GUIContent AggregationModeLabel = new GUIContent("\u805a\u5408\u6a21\u5f0f");
        private static readonly GUIContent EvaluatorLabel = new GUIContent("Evaluator");
        private static List<EvaluatorOption> evaluatorOptions;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return lineHeight + VerticalSpacing;
            }

            var evaluator = property.FindPropertyRelative(nameof(PFAttributeConfig.Evaluator));
            return lineHeight * 6f + VerticalSpacing * 6f + GetEvaluatorFieldsHeight(evaluator);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var id = property.FindPropertyRelative(nameof(PFAttributeConfig.Id));
            var name = property.FindPropertyRelative(nameof(PFAttributeConfig.Name));
            var comment = property.FindPropertyRelative(nameof(PFAttributeConfig.Comment));
            var defaultValue = property.FindPropertyRelative(nameof(PFAttributeConfig.DefaultValue));
            var aggregationMode = property.FindPropertyRelative(nameof(PFAttributeConfig.AggregationMode));
            var limitMinValue = property.FindPropertyRelative(nameof(PFAttributeConfig.LimitMinValue));
            var minValue = property.FindPropertyRelative(nameof(PFAttributeConfig.MinValue));
            var limitMaxValue = property.FindPropertyRelative(nameof(PFAttributeConfig.LimitMaxValue));
            var maxValue = property.FindPropertyRelative(nameof(PFAttributeConfig.MaxValue));
            var evaluator = property.FindPropertyRelative(nameof(PFAttributeConfig.Evaluator));

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
                    DrawValueLine(line, defaultValue, limitMinValue, minValue, limitMaxValue, maxValue);

                    line.y += line.height + VerticalSpacing;
                    EditorGUI.PropertyField(line, aggregationMode, AggregationModeLabel);

                    line.y += line.height + VerticalSpacing;
                    DrawEvaluator(ref line, evaluator);
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

        private static void DrawEvaluator(ref Rect line, SerializedProperty evaluator)
        {
            EnsureEvaluator(evaluator);

            var options = GetEvaluatorOptions();
            var selectedIndex = FindEvaluatorIndex(evaluator.managedReferenceValue?.GetType(), options);
            var labels = new GUIContent[options.Count];
            for (var i = 0; i < options.Count; i++)
            {
                labels[i] = options[i].Label;
            }

            var nextIndex = EditorGUI.Popup(line, EvaluatorLabel, selectedIndex, labels);
            if (nextIndex != selectedIndex)
            {
                evaluator.managedReferenceValue = Activator.CreateInstance(options[nextIndex].Type);
            }

            DrawEvaluatorFields(ref line, evaluator);
        }

        private static void DrawEvaluatorFields(ref Rect line, SerializedProperty evaluator)
        {
            var iterator = evaluator.Copy();
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

        private static float GetEvaluatorFieldsHeight(SerializedProperty evaluator)
        {
            if (evaluator == null || evaluator.managedReferenceValue == null)
            {
                return 0f;
            }

            var height = 0f;
            var iterator = evaluator.Copy();
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

        private static void EnsureEvaluator(SerializedProperty evaluator)
        {
            if (evaluator.managedReferenceValue != null)
            {
                return;
            }

            evaluator.managedReferenceValue = new PFDefaultAttributeEvaluatorConfig();
        }

        private static int FindEvaluatorIndex(Type type, IReadOnlyList<EvaluatorOption> options)
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

        private static List<EvaluatorOption> GetEvaluatorOptions()
        {
            if (evaluatorOptions != null)
            {
                return evaluatorOptions;
            }

            evaluatorOptions = new List<EvaluatorOption>();
            var types = TypeCache.GetTypesDerivedFrom<PFAttributeEvaluatorConfig>();
            foreach (var type in types)
            {
                if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                var instance = (PFAttributeEvaluatorConfig)Activator.CreateInstance(type);
                evaluatorOptions.Add(new EvaluatorOption(
                    type,
                    new GUIContent(instance.DisplayName)));
            }

            evaluatorOptions.Sort(CompareEvaluatorOptions);
            return evaluatorOptions;
        }

        private static int CompareEvaluatorOptions(EvaluatorOption a, EvaluatorOption b)
        {
            if (a.Type == typeof(PFDefaultAttributeEvaluatorConfig))
            {
                return b.Type == typeof(PFDefaultAttributeEvaluatorConfig) ? 0 : -1;
            }

            if (b.Type == typeof(PFDefaultAttributeEvaluatorConfig))
            {
                return 1;
            }

            return string.Compare(a.Label.text, b.Label.text, StringComparison.Ordinal);
        }

        private readonly struct EvaluatorOption
        {
            public EvaluatorOption(Type type, GUIContent label)
            {
                Type = type;
                Label = label;
            }

            public readonly Type Type;
            public readonly GUIContent Label;
        }
    }
}
