using System.Collections.Generic;
using System.Text;

namespace PFGAS.Editor
{
    public static class PFTagNameUtility
    {
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
            "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return",
            "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
            "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while",
        };

        public static string ToCodeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "_";
            }

            var builder = new StringBuilder();
            var trimmed = value.Trim();
            for (var i = 0; i < trimmed.Length; i++)
            {
                var ch = trimmed[i];
                builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
            }

            if (builder.Length == 0)
            {
                builder.Append('_');
            }

            if (char.IsDigit(builder[0]))
            {
                builder.Insert(0, '_');
            }

            var identifier = builder.ToString();
            return CSharpKeywords.Contains(identifier) ? identifier + "_" : identifier;
        }

        public static string ToCodeName(IReadOnlyList<string> segments)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < segments.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(ToCodeIdentifier(segments[i]));
            }

            return builder.Length == 0 ? "_" : builder.ToString();
        }

        public static List<string> GetPathSegments(PFTagExcelRow row, IReadOnlyDictionary<int, PFTagExcelRow> byId)
        {
            var result = new List<string>();
            var seen = new HashSet<int>();
            var current = row;

            while (current != null)
            {
                if (!seen.Add(current.Id))
                {
                    break;
                }

                result.Insert(0, current.Name);
                if (current.ParentId == PFTagExcelRow.RootParentId ||
                    !byId.TryGetValue(current.ParentId, out current))
                {
                    break;
                }
            }

            return result;
        }

        public static string BuildFullPath(PFTagExcelRow row, IReadOnlyDictionary<int, PFTagExcelRow> byId)
        {
            return string.Join(".", GetPathSegments(row, byId));
        }
    }
}
