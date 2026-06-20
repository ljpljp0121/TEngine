using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PFGAS.Editor
{
    public sealed class PFTagExcelRow
    {
        public const int RootParentId = -1;

        public int Id;
        public int ParentId = RootParentId;
        public string Name = string.Empty;
        public string Desc = string.Empty;
        public string FullPath = string.Empty;

        public PFTagExcelRow Clone()
        {
            return new PFTagExcelRow
            {
                Id = Id,
                ParentId = ParentId,
                Name = Name,
                Desc = Desc,
                FullPath = FullPath,
            };
        }
    }

    public sealed class PFTagExcelDocument
    {
        public string ExcelPath = string.Empty;
        public int IdColumn;
        public int ParentIdColumn;
        public int NameColumn;
        public int DescColumn;
        public int FullPathColumn;
        public int MaxColumn;
        public readonly List<PFTagExcelRow> Rows = new List<PFTagExcelRow>();
    }

    public sealed class PFTagValidationResult
    {
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;

        public bool IsValid => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }

        public string FormatErrors()
        {
            return string.Join(Environment.NewLine, errors);
        }
    }

    public sealed class PFTagExcelFileLockedException : IOException
    {
        public PFTagExcelFileLockedException(string path, Exception inner)
            : base($"PFTag Excel is locked or not writable: {path}", inner)
        {
        }
    }

    public static class PFTagExcelPaths
    {
        public const string TagExcelProjectPath = "Configs/GameConfig/Datas/PFTag.xlsx";

        public static string WorkspaceRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        public static string TagExcelPath =>
            Path.GetFullPath(Path.Combine(WorkspaceRoot, TagExcelProjectPath));
    }
}
