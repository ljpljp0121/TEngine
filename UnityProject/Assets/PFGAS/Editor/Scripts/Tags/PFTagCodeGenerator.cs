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
    /// 根据编辑器 Tag 配置生成 PFTag 常量和注册代码。
    /// </summary>
    public static class PFTagCodeGenerator
    {
        private const string FileName = "PFTagGenerated.cs";

        public static void GenerateCode()
        {
            var config = AssetDatabase.LoadAssetAtPath<PFTagConfig>(PFTagTreeWindow.ConfigPath);
            if (config == null)
            {
                Debug.LogError("未找到 PFTagConfig，请先在编辑器中配置 Tag 树");
                return;
            }

            string outputPath = GetOutputPath(config);
            GenerateFile(config, outputPath);

            AssetDatabase.Refresh();
            Debug.Log($"Tag 代码生成完成！输出路径：{outputPath}");
        }

        private static string GetOutputPath(PFTagConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.CodeGenPath))
            {
                return NormalizeOutputPath(config.CodeGenPath);
            }

            return GetDefaultOutputPath();
        }

        public static string GetDefaultOutputPath()
        {
            var guids = AssetDatabase.FindAssets("t:Script PFTagContainer");
            if (guids.Length > 0)
            {
                return NormalizeOutputPath(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0])));
            }

            return "Assets/PFPackage/PFTagSystem/Runtime";
        }

        private static string NormalizeOutputPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().Replace("\\", "/").TrimEnd('/');
        }

        private static int GetTagId(PFTagNodeConfig node)
        {
            return node.TagId > 0 ? node.TagId : node.Id;
        }

        private static void GenerateFile(PFTagConfig config, string outputPath)
        {
            var validNodes = config.Nodes.Where(n => n.Depth >= 0).ToList();
            if (validNodes.Count == 0)
            {
                return;
            }

            var childMap = BuildChildMap(validNodes);

            var sb = new StringBuilder();
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine("//// This is a generated file. ////");
            sb.AppendLine("////     Do not modify it.     ////");
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace PFGAS.Runtime");
            sb.AppendLine("{");
            sb.AppendLine("    public enum PFTagId");
            sb.AppendLine("    {");

            foreach (var node in validNodes)
            {
                int tagId = GetTagId(node);
                string enumName = GetEnumName(config, node);
                sb.AppendLine($"        {enumName} = {tagId},");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Registers generated PFTag hierarchy and display names.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class PFTagGenerated");
            sb.AppendLine("    {");
            sb.AppendLine("        static PFTagGenerated()");
            sb.AppendLine("        {");
            sb.AppendLine("            TagHelper.Register(new Dictionary<PFTagId, PFTag>");
            sb.AppendLine("            {");

            foreach (var node in validNodes)
            {
                string enumName = GetEnumName(config, node);
                string parents = FormatTagArray(GetParentTagNames(node, config));
                string children = FormatTagArray(
                    childMap.TryGetValue(node.Id, out var c)
                        ? c.Select(id => GetEnumName(config, config.Nodes.First(n => n.Id == id))).ToArray()
                        : Array.Empty<string>());
                string tagKey = FormatTagKey(enumName);
                sb.AppendLine($"                {{ {tagKey}, new PFTag({tagKey}, {parents}, {children}) }},");
            }

            sb.AppendLine("            });");
            sb.AppendLine();
            sb.AppendLine("            TagHelper.RegisterNames(new Dictionary<PFTagId, string>");
            sb.AppendLine("            {");

            foreach (var node in validNodes)
            {
                string enumName = GetEnumName(config, node);
                string pathName = GetPathName(config, node);
                sb.AppendLine($"                {{ {FormatTagKey(enumName)}, \"{pathName}\" }},");
            }

            sb.AppendLine("            });");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            Directory.CreateDirectory(outputPath);
            File.WriteAllText(Path.Combine(outputPath, FileName), sb.ToString());
        }

        private static Dictionary<int, List<int>> BuildChildMap(List<PFTagNodeConfig> nodes)
        {
            var map = new Dictionary<int, List<int>>();
            foreach (var node in nodes)
            {
                if (node.ParentId < 0)
                {
                    continue;
                }

                if (!map.TryGetValue(node.ParentId, out var list))
                {
                    list = new List<int>();
                    map[node.ParentId] = list;
                }

                list.Add(node.Id);
            }

            return map;
        }

        private static string[] GetParentTagNames(PFTagNodeConfig node, PFTagConfig config)
        {
            var parents = new List<string>();
            int parentId = node.ParentId;
            while (parentId >= 0)
            {
                var parent = config.Nodes.FirstOrDefault(n => n.Id == parentId);
                if (parent == null)
                {
                    break;
                }

                parents.Add(GetEnumName(config, parent));
                parentId = parent.ParentId;
            }

            return parents.ToArray();
        }

        private static string FormatTagArray(string[] tags)
        {
            if (tags.Length == 0)
            {
                return "Array.Empty<PFTagId>()";
            }

            return "new[] { " + string.Join(", ", tags.Select(FormatTagKey)) + " }";
        }

        private static string FormatTagKey(string enumName)
        {
            return $"PFTagId.{enumName}";
        }

        public static string GetEnumName(PFTagConfig config, PFTagNodeConfig node)
        {
            var names = new List<string> { node.Name };
            int parentId = node.ParentId;

            while (parentId >= 0)
            {
                var parent = config.Nodes.FirstOrDefault(n => n.Id == parentId);
                if (parent == null)
                {
                    break;
                }

                names.Insert(0, parent.Name);
                parentId = parent.ParentId;
            }

            return string.Join("_", names);
        }

        private static string GetPathName(PFTagConfig config, PFTagNodeConfig node)
        {
            var names = new List<string> { node.Name };
            int parentId = node.ParentId;

            while (parentId >= 0)
            {
                var parent = config.Nodes.FirstOrDefault(n => n.Id == parentId);
                if (parent == null)
                {
                    break;
                }

                names.Insert(0, parent.Name);
                parentId = parent.ParentId;
            }

            return string.Join(".", names);
        }
    }
}
