using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>根据属性配置资产生成 AttributeId 和全局 AttributeRule 代码。</summary>
    public static class PFAttributeCodeGenerator
    {
        private const string FileName = "PFAttributeGenerated.cs";

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum", "event", "explicit",
            "extern", "false", "finally", "fixed", "float", "for", "foreach",
            "goto", "if", "implicit", "in", "int", "interface", "internal",
            "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong",
            "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while"
        };

        public static void GenerateCode(PFAttributeConfigAsset config)
        {
            if (config == null)
            {
                Debug.LogError("PFAttributeConfig not found. Open the Attribute Config window first.");
                return;
            }

            if (!EnsureStableIds(config, out var assignedIds, out var idError))
            {
                Debug.LogError(idError);
                return;
            }

            if (!TryBuildModel(config, out var attributes, out var error))
            {
                Debug.LogError(error);
                return;
            }

            if (assignedIds)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            var outputPath = GetDefaultOutputPath();
            GenerateFile(attributes, outputPath);
            AssetDatabase.Refresh();
            Debug.Log($"Attribute code generated: {Path.Combine(outputPath, FileName).Replace("\\", "/")}");
        }

        public static bool EnsureStableIds(
            PFAttributeConfigAsset config,
            out bool changed,
            out string error)
        {
            return TryAssignStableIds(config, out changed, out error);
        }

        public static string GetDefaultOutputPath()
        {
            var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(FileName));
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(FileName, StringComparison.Ordinal))
                {
                    return NormalizeOutputPath(Path.GetDirectoryName(path));
                }
            }

            return "Assets/PFPackage/PFGAS/Runtime/Gen";
        }

        private static void GenerateFile(
            IReadOnlyList<AttributeModel> attributes,
            string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine("//// This is a generated file. ////");
            sb.AppendLine("////     Do not modify it.     ////");
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine();
            sb.AppendLine("namespace PFGAS.Runtime");
            sb.AppendLine("{");
            GenerateAttributeIdEnum(sb, attributes);
            sb.AppendLine();
            GenerateAttributeRulesClass(sb, attributes);
            sb.AppendLine("}");

            Directory.CreateDirectory(outputPath);
            File.WriteAllText(Path.Combine(outputPath, FileName), sb.ToString());
        }

        private static void GenerateAttributeIdEnum(
            StringBuilder sb,
            IReadOnlyList<AttributeModel> attributes)
        {
            sb.AppendLine("    public enum PFAttributeId");
            sb.AppendLine("    {");
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                AppendXmlSummary(sb, 8, attribute.Config.Comment);
                sb.AppendLine($"        {attribute.EnumName} = {attribute.Id},");
            }

            sb.AppendLine("    }");
        }

        private static void GenerateAttributeRulesClass(
            StringBuilder sb,
            IReadOnlyList<AttributeModel> attributes)
        {
            sb.AppendLine("    public static class PFAttributeRules");
            sb.AppendLine("    {");
            for (var i = 0; i < attributes.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine();
                }

                GenerateAttributeRuleField(sb, attributes[i]);
            }

            sb.AppendLine();
            sb.AppendLine("        private static readonly AttributeRule[] AllRules =");
            sb.AppendLine("        {");
            for (var i = 0; i < attributes.Count; i++)
            {
                sb.AppendLine($"            {attributes[i].RuleName},");
            }

            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        public static readonly System.Collections.ObjectModel.ReadOnlyCollection<AttributeRule> All =");
            sb.AppendLine("            System.Array.AsReadOnly(AllRules);");
            sb.AppendLine();
            sb.AppendLine("        public static AttributeRule Get(PFAttributeId attributeId)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch (attributeId)");
            sb.AppendLine("            {");
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                sb.AppendLine($"                case PFAttributeId.{attribute.EnumName}:");
                sb.AppendLine($"                    return {attribute.RuleName};");
            }

            sb.AppendLine("                default:");
            sb.AppendLine("                    throw new System.ArgumentOutOfRangeException(nameof(attributeId), attributeId, null);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        private static void GenerateAttributeRuleField(StringBuilder sb, AttributeModel attribute)
        {
            AppendXmlSummary(sb, 8, attribute.Config.Comment);
            sb.AppendLine($"        public static readonly AttributeRule {attribute.RuleName} =");
            sb.AppendLine("            new AttributeRule(");
            sb.AppendLine($"                PFAttributeId.{attribute.EnumName},");
            sb.AppendLine($"                {FormatFloat(attribute.Config.DefaultValue)},");
            sb.AppendLine($"                AggregationMode.{attribute.Config.AggregationMode},");
            sb.AppendLine($"                {FormatMinValue(attribute.Config)},");
            sb.AppendLine($"                {FormatMaxValue(attribute.Config)},");
            sb.AppendLine($"                {attribute.BaseValueProcessorExpression},");
            sb.AppendLine($"                {attribute.CurrentValueProcessorExpression});");
        }

        private static bool TryBuildModel(
            PFAttributeConfigAsset config,
            out List<AttributeModel> attributes,
            out string error)
        {
            attributes = new List<AttributeModel>();
            error = null;

            if (config.Attributes == null || config.Attributes.Length == 0)
            {
                error = "Attribute config has no attributes.";
                return false;
            }

            var attributeNames = new HashSet<string>(StringComparer.Ordinal);
            var attributeIds = new HashSet<int>();
            var attributesById =
                new Dictionary<int, PFAttributeGenerationAttributeInfo>();

            for (var attributeIndex = 0; attributeIndex < config.Attributes.Length; attributeIndex++)
            {
                var attributeConfig = config.Attributes[attributeIndex];
                if (attributeConfig == null)
                {
                    error = $"Attribute at index {attributeIndex} is null.";
                    return false;
                }

                var attributeName = NormalizeName(attributeConfig.Name);
                if (!IsValidIdentifier(attributeName))
                {
                    error = $"Attribute name '{attributeConfig.Name}' is not a valid C# identifier.";
                    return false;
                }

                if (!attributeNames.Add(attributeName))
                {
                    error = $"Duplicate attribute name '{attributeName}'.";
                    return false;
                }

                if (attributeConfig.Id < 0)
                {
                    error = $"Attribute '{attributeName}' has invalid Id '{attributeConfig.Id}'.";
                    return false;
                }

                if (!attributeIds.Add(attributeConfig.Id))
                {
                    error = $"Duplicate attribute id '{attributeConfig.Id}'.";
                    return false;
                }

                if (!ValidateValueRange(attributeConfig, out error))
                {
                    error = $"Attribute '{attributeName}' is invalid: {error}";
                    return false;
                }

                var info = new PFAttributeGenerationAttributeInfo(attributeName, attributeConfig.Id);
                var model = new AttributeModel(info, attributeConfig);
                attributesById.Add(attributeConfig.Id, info);
                attributes.Add(model);
            }

            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                EnsureProcessorConfigs(attribute.Config);
                var context = new PFAttributeProcessorCodeContext(attribute.Info, attributesById);
                if (!attribute.Config.BaseValueProcessor.TryBuildProcessorExpression(
                        context,
                        out var baseValueProcessorExpression,
                        out error))
                {
                    error = $"Attribute '{attribute.Name}' is invalid: {error}";
                    return false;
                }

                if (!attribute.Config.CurrentValueProcessor.TryBuildProcessorExpression(
                        context,
                        out var currentValueProcessorExpression,
                        out error))
                {
                    error = $"Attribute '{attribute.Name}' is invalid: {error}";
                    return false;
                }

                attribute.BaseValueProcessorExpression = baseValueProcessorExpression;
                attribute.CurrentValueProcessorExpression = currentValueProcessorExpression;
            }

            return true;
        }

        private static bool TryAssignStableIds(
            PFAttributeConfigAsset config,
            out bool changed,
            out string error)
        {
            changed = false;
            error = null;

            if (config.Attributes == null)
            {
                return true;
            }

            if (config.MaxId < 0)
            {
                config.MaxId = 0;
                changed = true;
            }

            var usedAttributeIds = new HashSet<int>();
            for (var attributeIndex = 0; attributeIndex < config.Attributes.Length; attributeIndex++)
            {
                var attributeConfig = config.Attributes[attributeIndex];
                if (attributeConfig == null)
                {
                    continue;
                }

                if (attributeConfig.BaseValueProcessor == null)
                {
                    attributeConfig.BaseValueProcessor = new PFDefaultAttributeBaseValueProcessorConfig();
                    changed = true;
                }

                if (attributeConfig.CurrentValueProcessor == null)
                {
                    attributeConfig.CurrentValueProcessor = new PFDefaultAttributeCurrentValueProcessorConfig();
                    changed = true;
                }

                if (attributeConfig.Id < 0 || usedAttributeIds.Contains(attributeConfig.Id))
                {
                    var attributeId = AllocateStableId(ref config.MaxId, usedAttributeIds);
                    attributeConfig.Id = attributeId;
                    changed = true;
                }
                else
                {
                    usedAttributeIds.Add(attributeConfig.Id);
                    if (config.MaxId <= attributeConfig.Id)
                    {
                        config.MaxId = attributeConfig.Id + 1;
                        changed = true;
                    }
                }
            }

            return true;
        }

        private static int AllocateStableId(ref int nextId, HashSet<int> usedIds)
        {
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            var id = nextId;
            usedIds.Add(id);
            nextId++;
            return id;
        }

        private static bool ValidateValueRange(
            PFAttributeConfig config,
            out string error)
        {
            error = null;
            if (!IsFinite(config.DefaultValue))
            {
                error = "DefaultValue must be finite.";
                return false;
            }

            if (config.LimitMinValue && !IsFinite(config.MinValue))
            {
                error = "MinValue must be finite.";
                return false;
            }

            if (config.LimitMaxValue && !IsFinite(config.MaxValue))
            {
                error = "MaxValue must be finite.";
                return false;
            }

            if (config.LimitMinValue &&
                config.LimitMaxValue &&
                config.MinValue > config.MaxValue)
            {
                error = "MinValue cannot be greater than MaxValue.";
                return false;
            }

            if (config.LimitMinValue && config.DefaultValue < config.MinValue)
            {
                error = "DefaultValue cannot be less than MinValue.";
                return false;
            }

            if (config.LimitMaxValue && config.DefaultValue > config.MaxValue)
            {
                error = "DefaultValue cannot be greater than MaxValue.";
                return false;
            }

            return true;
        }

        private static void EnsureProcessorConfigs(PFAttributeConfig attribute)
        {
            if (attribute.BaseValueProcessor == null)
            {
                attribute.BaseValueProcessor = new PFDefaultAttributeBaseValueProcessorConfig();
            }

            if (attribute.CurrentValueProcessor == null)
            {
                attribute.CurrentValueProcessor = new PFDefaultAttributeCurrentValueProcessorConfig();
            }
        }

        private static string FormatMinValue(PFAttributeConfig config)
        {
            return config.LimitMinValue ? FormatFloat(config.MinValue) : "float.MinValue";
        }

        private static string FormatMaxValue(PFAttributeConfig config)
        {
            return config.LimitMaxValue ? FormatFloat(config.MaxValue) : "float.MaxValue";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture) + "f";
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || CSharpKeywords.Contains(value))
            {
                return false;
            }

            if (!IsIdentifierStart(value[0]))
            {
                return false;
            }

            for (var i = 1; i < value.Length; i++)
            {
                if (!IsIdentifierPart(value[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsIdentifierStart(char value)
        {
            return value == '_' || value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
        }

        private static bool IsIdentifierPart(char value)
        {
            return IsIdentifierStart(value) || value >= '0' && value <= '9';
        }

        private static string NormalizeName(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        private static string NormalizeOutputPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? "Assets/PFPackage/PFGAS/Runtime/Gen"
                : path.Trim().Replace("\\", "/").TrimEnd('/');
        }

        private static void AppendXmlSummary(StringBuilder sb, int indent, string value)
        {
            value = NormalizeName(value);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            value = value.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = value.Split('\n');
            if (lines.Length == 1)
            {
                sb.Append(' ', indent);
                sb.AppendLine($"/// <summary>{EscapeXml(lines[0].Trim())}</summary>");
                return;
            }

            sb.Append(' ', indent);
            sb.AppendLine("/// <summary>");
            for (var i = 0; i < lines.Length; i++)
            {
                sb.Append(' ', indent);
                sb.AppendLine($"/// {EscapeXml(lines[i].Trim())}");
            }

            sb.Append(' ', indent);
            sb.AppendLine("/// </summary>");
        }

        private static string EscapeXml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>代码生成时使用的单条属性模型。</summary>
        private sealed class AttributeModel
        {
            public AttributeModel(
                PFAttributeGenerationAttributeInfo info,
                PFAttributeConfig config)
            {
                Info = info;
                Config = config;
            }

            public readonly PFAttributeGenerationAttributeInfo Info;
            public readonly PFAttributeConfig Config;
            public string BaseValueProcessorExpression;
            public string CurrentValueProcessorExpression;
            public string Name => Info.Name;
            public int Id => Info.Id;
            public string EnumName => Info.EnumName;
            public string RuleName => Info.RuleName;
        }
    }
}
