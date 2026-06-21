using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>根据属性配置资产生成 AttributeId、AttributeSetId 和 AttributeSet 代码。</summary>
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
                Debug.LogError("PFAttributeConfig not found. Open the Attribute window first.");
                return;
            }

            if (!EnsureStableIds(config, out var assignedIds, out var idError))
            {
                Debug.LogError(idError);
                return;
            }

            if (!TryBuildModel(config, out var attributes, out var attributeSets, out var error))
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
            GenerateFile(attributes, attributeSets, outputPath);
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

            return "Assets/PFGAS/Runtime/Gen";
        }

        private static void GenerateFile(
            IReadOnlyList<AttributeModel> attributes,
            IReadOnlyList<AttributeSetModel> attributeSets,
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
            GenerateAttributeSetIdEnum(sb, attributeSets);
            sb.AppendLine();
            GenerateAttributeSetsClass(sb, attributeSets);
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

        private static void GenerateAttributeSetIdEnum(
            StringBuilder sb,
            IReadOnlyList<AttributeSetModel> attributeSets)
        {
            sb.AppendLine("    public enum PFAttributeSetId");
            sb.AppendLine("    {");
            for (var i = 0; i < attributeSets.Count; i++)
            {
                var attributeSet = attributeSets[i];
                AppendXmlSummary(sb, 8, attributeSet.Config.Comment);
                sb.AppendLine($"        {attributeSet.EnumName} = {attributeSet.Id},");
            }

            sb.AppendLine("    }");
        }

        private static void GenerateAttributeSetsClass(
            StringBuilder sb,
            IReadOnlyList<AttributeSetModel> attributeSets)
        {
            sb.AppendLine("    public static class PFAttributeSets");
            sb.AppendLine("    {");
            for (var i = 0; i < attributeSets.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine();
                }

                GenerateAttributeSetField(sb, attributeSets[i]);
            }

            sb.AppendLine();
            sb.AppendLine("        private static readonly AttributeSet[] AllSets =");
            sb.AppendLine("        {");
            for (var i = 0; i < attributeSets.Count; i++)
            {
                sb.AppendLine($"            {attributeSets[i].FieldName},");
            }

            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        public static readonly System.Collections.ObjectModel.ReadOnlyCollection<AttributeSet> All =");
            sb.AppendLine("            System.Array.AsReadOnly(AllSets);");
            sb.AppendLine();
            sb.AppendLine("        public static AttributeSet Get(PFAttributeSetId attributeSetId)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch (attributeSetId)");
            sb.AppendLine("            {");
            for (var i = 0; i < attributeSets.Count; i++)
            {
                var attributeSet = attributeSets[i];
                sb.AppendLine($"                case PFAttributeSetId.{attributeSet.EnumName}:");
                sb.AppendLine($"                    return {attributeSet.FieldName};");
            }

            sb.AppendLine("                default:");
            sb.AppendLine("                    throw new System.ArgumentOutOfRangeException(nameof(attributeSetId), attributeSetId, null);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        private static void GenerateAttributeSetField(StringBuilder sb, AttributeSetModel attributeSet)
        {
            AppendXmlSummary(sb, 8, attributeSet.Config.Comment);
            sb.AppendLine($"        public static readonly AttributeSet {attributeSet.FieldName} =");
            sb.AppendLine("            new AttributeSet(");
            sb.AppendLine($"                (int)PFAttributeSetId.{attributeSet.EnumName},");
            sb.AppendLine($"                nameof(PFAttributeSetId.{attributeSet.EnumName}),");
            sb.AppendLine("                new[]");
            sb.AppendLine("                {");
            for (var i = 0; i < attributeSet.Entries.Count; i++)
            {
                GenerateAttributeSetEntryExpression(sb, attributeSet.Entries[i], i == attributeSet.Entries.Count - 1);
            }

            sb.AppendLine("                });");
        }

        private static void GenerateAttributeSetEntryExpression(
            StringBuilder sb,
            AttributeSetEntryModel entry,
            bool isLast)
        {
            sb.AppendLine("                    new AttributeSetEntry(");
            sb.AppendLine($"                        PFAttributeId.{entry.Attribute.EnumName},");
            sb.AppendLine($"                        {FormatFloat(entry.Config.DefaultValue)},");
            sb.AppendLine($"                        AggregationMode.{entry.Config.AggregationMode},");
            sb.AppendLine($"                        {FormatMinValue(entry.Config)},");
            sb.AppendLine($"                        {FormatMaxValue(entry.Config)},");
            sb.AppendLine($"                        {entry.BaseValueProcessorExpression},");
            sb.AppendLine($"                        {entry.CurrentValueProcessorExpression}){(isLast ? string.Empty : ",")}");
        }

        private static bool TryBuildModel(
            PFAttributeConfigAsset config,
            out List<AttributeModel> attributes,
            out List<AttributeSetModel> attributeSets,
            out string error)
        {
            attributes = new List<AttributeModel>();
            attributeSets = new List<AttributeSetModel>();
            error = null;

            if (!TryBuildAttributes(config, attributes, out var attributesById, out error))
            {
                return false;
            }

            return TryBuildAttributeSets(config, attributesById, attributeSets, out error);
        }

        private static bool TryBuildAttributes(
            PFAttributeConfigAsset config,
            List<AttributeModel> attributes,
            out Dictionary<int, PFAttributeGenerationAttributeInfo> attributesById,
            out string error)
        {
            attributesById = new Dictionary<int, PFAttributeGenerationAttributeInfo>();
            error = null;

            if (config.Attributes == null || config.Attributes.Length == 0)
            {
                error = "Attribute config has no attributes.";
                return false;
            }

            var attributeNames = new HashSet<string>(StringComparer.Ordinal);
            var attributeIds = new HashSet<int>();
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

                var info = new PFAttributeGenerationAttributeInfo(attributeName, attributeConfig.Id);
                attributesById.Add(attributeConfig.Id, info);
                attributes.Add(new AttributeModel(info, attributeConfig));
            }

            return true;
        }

        private static bool TryBuildAttributeSets(
            PFAttributeConfigAsset config,
            IReadOnlyDictionary<int, PFAttributeGenerationAttributeInfo> attributesById,
            List<AttributeSetModel> attributeSets,
            out string error)
        {
            error = null;
            if (config.AttributeSets == null || config.AttributeSets.Length == 0)
            {
                error = "Attribute config has no attribute sets.";
                return false;
            }

            var setNames = new HashSet<string>(StringComparer.Ordinal);
            var setIds = new HashSet<int>();
            for (var setIndex = 0; setIndex < config.AttributeSets.Length; setIndex++)
            {
                var setConfig = config.AttributeSets[setIndex];
                if (setConfig == null)
                {
                    error = $"AttributeSet at index {setIndex} is null.";
                    return false;
                }

                var setName = NormalizeName(setConfig.Name);
                if (!IsValidIdentifier(setName))
                {
                    error = $"AttributeSet name '{setConfig.Name}' is not a valid C# identifier.";
                    return false;
                }

                if (!setNames.Add(setName))
                {
                    error = $"Duplicate AttributeSet name '{setName}'.";
                    return false;
                }

                if (setConfig.Id < 0)
                {
                    error = $"AttributeSet '{setName}' has invalid Id '{setConfig.Id}'.";
                    return false;
                }

                if (!setIds.Add(setConfig.Id))
                {
                    error = $"Duplicate AttributeSet id '{setConfig.Id}'.";
                    return false;
                }

                var setModel = new AttributeSetModel(setConfig);
                if (!TryBuildAttributeSetEntries(setModel, attributesById, out error))
                {
                    error = $"AttributeSet '{setName}' is invalid: {error}";
                    return false;
                }

                attributeSets.Add(setModel);
            }

            return true;
        }

        private static bool TryBuildAttributeSetEntries(
            AttributeSetModel setModel,
            IReadOnlyDictionary<int, PFAttributeGenerationAttributeInfo> attributesById,
            out string error)
        {
            error = null;
            var entries = setModel.Config.Attributes;
            if (entries == null || entries.Length == 0)
            {
                error = "AttributeSet must contain at least one attribute.";
                return false;
            }

            var idsInSet = new HashSet<int>();
            for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                var entryConfig = entries[entryIndex];
                if (entryConfig == null)
                {
                    error = $"Entry at index {entryIndex} is null.";
                    return false;
                }

                if (!attributesById.TryGetValue(entryConfig.AttributeId, out var attribute))
                {
                    error = $"Entry at index {entryIndex} references unknown attribute id '{entryConfig.AttributeId}'.";
                    return false;
                }

                if (!idsInSet.Add(entryConfig.AttributeId))
                {
                    error = $"Attribute '{attribute.Name}' is duplicated in the set.";
                    return false;
                }

                if (!ValidateValueRange(entryConfig, out error))
                {
                    error = $"Attribute '{attribute.Name}' is invalid: {error}";
                    return false;
                }

                EnsureProcessorConfigs(entryConfig);
                setModel.Entries.Add(new AttributeSetEntryModel(attribute, entryConfig));
            }

            for (var i = 0; i < setModel.Entries.Count; i++)
            {
                var entry = setModel.Entries[i];
                var context = new PFAttributeProcessorCodeContext(entry.Attribute, attributesById, idsInSet);
                if (!entry.Config.BaseValueProcessor.TryBuildProcessorExpression(
                        context,
                        out var baseValueProcessorExpression,
                        out error))
                {
                    error = $"Attribute '{entry.Attribute.Name}' is invalid: {error}";
                    return false;
                }

                if (!entry.Config.CurrentValueProcessor.TryBuildProcessorExpression(
                        context,
                        out var currentValueProcessorExpression,
                        out error))
                {
                    error = $"Attribute '{entry.Attribute.Name}' is invalid: {error}";
                    return false;
                }

                entry.BaseValueProcessorExpression = baseValueProcessorExpression;
                entry.CurrentValueProcessorExpression = currentValueProcessorExpression;
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
                config.Attributes = Array.Empty<PFAttributeConfig>();
                changed = true;
            }

            if (config.AttributeSets == null)
            {
                config.AttributeSets = Array.Empty<PFAttributeSetConfig>();
                changed = true;
            }

            if (config.MaxId < 0)
            {
                config.MaxId = 0;
                changed = true;
            }

            if (config.MaxSetId < 0)
            {
                config.MaxSetId = 0;
                changed = true;
            }

            AssignAttributeIds(config, ref changed);
            AssignAttributeSetIds(config, ref changed);
            return true;
        }

        private static void AssignAttributeIds(PFAttributeConfigAsset config, ref bool changed)
        {
            var usedAttributeIds = new HashSet<int>();
            for (var attributeIndex = 0; attributeIndex < config.Attributes.Length; attributeIndex++)
            {
                var attributeConfig = config.Attributes[attributeIndex];
                if (attributeConfig == null)
                {
                    continue;
                }

                if (attributeConfig.Id < 0 || usedAttributeIds.Contains(attributeConfig.Id))
                {
                    attributeConfig.Id = AllocateStableId(ref config.MaxId, usedAttributeIds);
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
        }

        private static void AssignAttributeSetIds(PFAttributeConfigAsset config, ref bool changed)
        {
            var usedSetIds = new HashSet<int>();
            for (var setIndex = 0; setIndex < config.AttributeSets.Length; setIndex++)
            {
                var setConfig = config.AttributeSets[setIndex];
                if (setConfig == null)
                {
                    continue;
                }

                if (setConfig.Id < 0 || usedSetIds.Contains(setConfig.Id))
                {
                    setConfig.Id = AllocateStableId(ref config.MaxSetId, usedSetIds);
                    changed = true;
                }
                else
                {
                    usedSetIds.Add(setConfig.Id);
                    if (config.MaxSetId <= setConfig.Id)
                    {
                        config.MaxSetId = setConfig.Id + 1;
                        changed = true;
                    }
                }

                if (setConfig.Attributes == null)
                {
                    setConfig.Attributes = Array.Empty<PFAttributeSetEntryConfig>();
                    changed = true;
                    continue;
                }

                for (var entryIndex = 0; entryIndex < setConfig.Attributes.Length; entryIndex++)
                {
                    var entry = setConfig.Attributes[entryIndex];
                    if (entry == null)
                    {
                        continue;
                    }

                    if (entry.BaseValueProcessor == null)
                    {
                        entry.BaseValueProcessor = new PFDefaultAttributeBaseValueProcessorConfig();
                        changed = true;
                    }

                    if (entry.CurrentValueProcessor == null)
                    {
                        entry.CurrentValueProcessor = new PFDefaultAttributeCurrentValueProcessorConfig();
                        changed = true;
                    }
                }
            }
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
            PFAttributeSetEntryConfig config,
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

        private static void EnsureProcessorConfigs(PFAttributeSetEntryConfig entry)
        {
            if (entry.BaseValueProcessor == null)
            {
                entry.BaseValueProcessor = new PFDefaultAttributeBaseValueProcessorConfig();
            }

            if (entry.CurrentValueProcessor == null)
            {
                entry.CurrentValueProcessor = new PFDefaultAttributeCurrentValueProcessorConfig();
            }
        }

        private static string FormatMinValue(PFAttributeSetEntryConfig config)
        {
            return config.LimitMinValue ? FormatFloat(config.MinValue) : "float.MinValue";
        }

        private static string FormatMaxValue(PFAttributeSetEntryConfig config)
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
                ? "Assets/PFGAS/Runtime/Gen"
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
            public int Id => Info.Id;
            public string EnumName => Info.EnumName;
        }

        private sealed class AttributeSetModel
        {
            public AttributeSetModel(PFAttributeSetConfig config)
            {
                Config = config;
                Name = NormalizeName(config.Name);
            }

            public readonly PFAttributeSetConfig Config;
            public readonly List<AttributeSetEntryModel> Entries = new List<AttributeSetEntryModel>();
            public readonly string Name;
            public int Id => Config.Id;
            public string EnumName => Name;
            public string FieldName => EnumName;
        }

        private sealed class AttributeSetEntryModel
        {
            public AttributeSetEntryModel(
                PFAttributeGenerationAttributeInfo attribute,
                PFAttributeSetEntryConfig config)
            {
                Attribute = attribute;
                Config = config;
            }

            public readonly PFAttributeGenerationAttributeInfo Attribute;
            public readonly PFAttributeSetEntryConfig Config;
            public string BaseValueProcessorExpression;
            public string CurrentValueProcessorExpression;
        }
    }
}
