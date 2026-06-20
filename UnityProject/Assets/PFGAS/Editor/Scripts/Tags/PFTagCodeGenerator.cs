using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    /// <summary>
    /// 从 Luban Tag Excel 生成 Luban 注册适配层。
    /// </summary>
    public static class PFTagCodeGenerator
    {
        private const string AdapterFolder = "Assets/GameScripts/HotFix/PFGASGenerated/PFGAS";
        private const string AdapterAsmdefPath = "Assets/GameScripts/HotFix/PFGASGenerated/PFGASGenerated.asmdef";
        private const string AdapterFileName = "PFGASTagGenerated.cs";

        public static void GenerateCode()
        {
            var document = new PFTagExcelService().Read();
            GenerateCode(document.Rows);
            AssetDatabase.Refresh();
            Debug.Log("PFGAS Tag 适配代码生成完成。");
        }

        public static void GenerateCode(IReadOnlyList<PFTagExcelRow> rows)
        {
            var validation = new PFTagExcelValidator().Validate(rows);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Tag 数据校验失败，无法生成适配代码：\n" + validation.FormatErrors());
            }

            Directory.CreateDirectory(ToAbsoluteAssetPath(AdapterFolder));

            WriteIfChanged(Path.Combine(ToAbsoluteAssetPath(AdapterFolder), AdapterFileName), GenerateAdapterSource(rows));
            WriteIfChanged(ToAbsoluteAssetPath(AdapterAsmdefPath), GenerateAsmdefSource());
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void WriteIfChanged(string path, string content)
        {
            content = content.Replace("\r\n", "\n").Replace("\n", "\r\n");
            if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == content)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static string GenerateAdapterSource(IReadOnlyList<PFTagExcelRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine("//// This is a generated file. ////");
            sb.AppendLine("////     Do not modify it.     ////");
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using GameConfig;");
            sb.AppendLine("using PFGAS.Runtime;");
            sb.AppendLine("using LubanTag = GameConfig.PFGAS.PFTag;");
            sb.AppendLine("using RuntimeTag = PFGAS.Runtime.PFTag;");
            sb.AppendLine();
            sb.AppendLine("namespace PFGAS.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    public static class PFGASTagGenerated");
            sb.AppendLine("    {");
            sb.AppendLine("        public static void RegisterFromLubanTable()");
            sb.AppendLine("        {");
            sb.AppendLine("            RegisterFromLubanRows(Tables.GetTable<GameConfig.PFGAS.TbPFTag>().DataList);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static void RegisterFromLubanRows(IEnumerable<LubanTag> rows)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (rows == null)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw new ArgumentNullException(nameof(rows));");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            var rowList = rows.ToList();");
            sb.AppendLine("            var byId = rowList.ToDictionary(r => r.Id);");
            sb.AppendLine("            var childMap = rowList");
            sb.AppendLine("                .GroupBy(r => r.ParentId)");
            sb.AppendLine("                .ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToArray());");
            sb.AppendLine("            var tags = new Dictionary<PFTagId, RuntimeTag>();");
            sb.AppendLine("            var names = new Dictionary<PFTagId, string>();");
            sb.AppendLine();
            sb.AppendLine("            foreach (var row in rowList)");
            sb.AppendLine("            {");
            sb.AppendLine("                var tagId = new PFTagId(row.Id);");
            sb.AppendLine("                tags[tagId] = new RuntimeTag(");
            sb.AppendLine("                    tagId,");
            sb.AppendLine("                    GetParentIds(row, byId),");
            sb.AppendLine("                    childMap.TryGetValue(row.Id, out var children)");
            sb.AppendLine("                        ? children.Select(id => new PFTagId(id)).ToArray()");
            sb.AppendLine("                        : Array.Empty<PFTagId>());");
            sb.AppendLine("                names[tagId] = GetFullPath(row, byId);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            TagHelper.Clear();");
            sb.AppendLine("            TagHelper.Register(tags);");
            sb.AppendLine("            TagHelper.RegisterNames(names);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static PFTagId[] GetParentIds(LubanTag row, IReadOnlyDictionary<int, LubanTag> byId)");
            sb.AppendLine("        {");
            sb.AppendLine("            var result = new List<PFTagId>();");
            sb.AppendLine("            var parentId = row.ParentId;");
            sb.AppendLine("            while (parentId != -1 && byId.TryGetValue(parentId, out var parent))");
            sb.AppendLine("            {");
            sb.AppendLine("                result.Add(new PFTagId(parent.Id));");
            sb.AppendLine("                parentId = parent.ParentId;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            return result.ToArray();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static string GetFullPath(LubanTag row, IReadOnlyDictionary<int, LubanTag> byId)");
            sb.AppendLine("        {");
            sb.AppendLine("            var segments = new List<string>();");
            sb.AppendLine("            var current = row;");
            sb.AppendLine("            while (current != null)");
            sb.AppendLine("            {");
            sb.AppendLine("                segments.Insert(0, current.Name);");
            sb.AppendLine("                if (current.ParentId == -1 || !byId.TryGetValue(current.ParentId, out current))");
            sb.AppendLine("                {");
            sb.AppendLine("                    break;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            return string.Join(\".\", segments);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateAsmdefSource()
        {
            return @"{
    ""name"": ""PFGASGenerated"",
    ""rootNamespace"": """",
    ""references"": [
        ""com.peifeng.pfgas.Runtime"",
        ""GameProto""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}
";
        }
    }
}
