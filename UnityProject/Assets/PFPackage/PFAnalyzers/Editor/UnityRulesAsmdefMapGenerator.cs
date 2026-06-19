#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class UnityRulesAsmdefMapGenerator
{
    private static string OutputPath
    {
        get
        {
            var guids = AssetDatabase.FindAssets("t:Script UnityRulesAsmdefMapGenerator");
            if (guids.Length <= 0) return "";

            string basePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string path = Path.GetDirectoryName(basePath);
            return Path.Combine(path, "UnityRulesAsmdefMap.UnityRules.additionalfile").Replace("\\", "/");
        }
    }

    static UnityRulesAsmdefMapGenerator()
    {
        EditorApplication.delayCall += GenerateIfNeeded;
    }

    public static void GenerateIfNeeded()
    {
        try
        {
            var content = BuildContent();
            var outputDirectory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            if (File.Exists(OutputPath) && File.ReadAllText(OutputPath, Encoding.UTF8) == content)
            {
                return;
            }

            File.WriteAllText(OutputPath, content, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static string BuildContent()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# UnityRules asmdef map");
        builder.AppendLine("# name path includePlatforms excludePlatforms defineConstraints optionalUnityReferences");

        foreach (var asmdefPath in EnumerateAsmdefPaths())
        {
            var json = File.ReadAllText(asmdefPath, Encoding.UTF8);
            var asmdef = JsonUtility.FromJson<AssemblyDefinitionData>(json);
            if (asmdef == null || string.IsNullOrWhiteSpace(asmdef.name))
            {
                continue;
            }

            builder
                .Append(Sanitize(asmdef.name)).Append('\t')
                .Append(Sanitize(NormalizePath(asmdefPath))).Append('\t')
                .Append(Join(asmdef.includePlatforms)).Append('\t')
                .Append(Join(asmdef.excludePlatforms)).Append('\t')
                .Append(Join(asmdef.defineConstraints)).Append('\t')
                .Append(Join(asmdef.optionalUnityReferences)).AppendLine();
        }

        return builder.ToString();
    }

    private static IEnumerable<string> EnumerateAsmdefPaths()
    {
        foreach (var root in new[] { "Assets", "Packages" })
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var path in Directory.GetFiles(root, "*.asmdef", SearchOption.AllDirectories))
            {
                yield return NormalizePath(path);
            }
        }
    }

    private static string Join(string[] values)
    {
        if (values == null || values.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(";", Array.ConvertAll(values, Sanitize));
    }

    private static string Sanitize(string value)
    {
        return (value ?? string.Empty)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    [Serializable]
    private sealed class AssemblyDefinitionData
    {
        public string name = string.Empty;
        public string[] includePlatforms = Array.Empty<string>();
        public string[] excludePlatforms = Array.Empty<string>();
        public string[] defineConstraints = Array.Empty<string>();
        public string[] optionalUnityReferences = Array.Empty<string>();
    }
}

internal sealed class UnityRulesAsmdefMapAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (ContainsAsmdef(importedAssets)
            || ContainsAsmdef(deletedAssets)
            || ContainsAsmdef(movedAssets)
            || ContainsAsmdef(movedFromAssetPaths))
        {
            EditorApplication.delayCall += UnityRulesAsmdefMapGenerator.GenerateIfNeeded;
        }
    }

    private static bool ContainsAsmdef(IEnumerable<string> assetPaths)
    {
        foreach (var assetPath in assetPaths)
        {
            if (assetPath.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
#endif
