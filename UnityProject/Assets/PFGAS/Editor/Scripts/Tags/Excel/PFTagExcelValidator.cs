using System.Collections.Generic;
using System.Linq;

namespace PFGAS.Editor
{
    public sealed class PFTagExcelValidator
    {
        public PFTagValidationResult Validate(IReadOnlyList<PFTagExcelRow> rows)
        {
            var result = new PFTagValidationResult();
            ValidateIds(rows, result);

            var uniqueRows = rows
                .GroupBy(r => r.Id)
                .Where(g => g.Count() == 1)
                .Select(g => g.First())
                .ToList();
            var byId = uniqueRows.ToDictionary(r => r.Id, r => r);

            ValidateParents(uniqueRows, byId, result);
            ValidateSiblingNames(uniqueRows, byId, result);
            ValidateCycles(uniqueRows, byId, result);
            ValidateGeneratedNames(uniqueRows, byId, result);
            return result;
        }

        private static void ValidateIds(IReadOnlyList<PFTagExcelRow> rows, PFTagValidationResult result)
        {
            foreach (var group in rows.GroupBy(r => r.Id).Where(g => g.Count() > 1))
            {
                result.AddError($"Tag ID 重复：{group.Key}");
            }
        }

        private static void ValidateParents(
            IReadOnlyList<PFTagExcelRow> rows,
            IReadOnlyDictionary<int, PFTagExcelRow> byId,
            PFTagValidationResult result)
        {
            foreach (var row in rows)
            {
                if (row.ParentId == PFTagExcelRow.RootParentId)
                {
                    continue;
                }

                if (!byId.ContainsKey(row.ParentId))
                {
                    result.AddError($"Tag `{row.Name}` 的 ParentId `{row.ParentId}` 不存在。");
                }
            }
        }

        private static void ValidateSiblingNames(
            IReadOnlyList<PFTagExcelRow> rows,
            IReadOnlyDictionary<int, PFTagExcelRow> byId,
            PFTagValidationResult result)
        {
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    result.AddError($"Tag `{row.Id}` 的 Name 不能为空。");
                    continue;
                }

                if (PFTagNameUtility.ToCodeIdentifier(row.Name) != row.Name)
                {
                    result.AddError($"Tag `{PFTagNameUtility.BuildFullPath(row, byId)}` 的短名不能生成合法代码标识符。");
                }
            }

            foreach (var group in rows.GroupBy(r => r.ParentId))
            {
                foreach (var nameGroup in group.GroupBy(r => r.Name).Where(g => g.Count() > 1))
                {
                    var paths = nameGroup.Select(r => PFTagNameUtility.BuildFullPath(r, byId));
                    result.AddError($"同父级短名重复：{string.Join(", ", paths)}");
                }
            }
        }

        private static void ValidateCycles(
            IReadOnlyList<PFTagExcelRow> rows,
            IReadOnlyDictionary<int, PFTagExcelRow> byId,
            PFTagValidationResult result)
        {
            foreach (var row in rows)
            {
                var seen = new HashSet<int> { row.Id };
                var parentId = row.ParentId;
                while (parentId != PFTagExcelRow.RootParentId && byId.TryGetValue(parentId, out var parent))
                {
                    if (!seen.Add(parent.Id))
                    {
                        result.AddError($"Tag 父子关系存在循环：{PFTagNameUtility.BuildFullPath(row, byId)}");
                        break;
                    }

                    parentId = parent.ParentId;
                }
            }
        }

        private static void ValidateGeneratedNames(
            IReadOnlyList<PFTagExcelRow> rows,
            IReadOnlyDictionary<int, PFTagExcelRow> byId,
            PFTagValidationResult result)
        {
            var names = new Dictionary<string, PFTagExcelRow>();
            foreach (var row in rows)
            {
                var codeName = PFTagNameUtility.ToCodeName(PFTagNameUtility.GetPathSegments(row, byId));
                if (names.TryGetValue(codeName, out var existing))
                {
                    result.AddError(
                        $"生成名 `{codeName}` 冲突：{PFTagNameUtility.BuildFullPath(existing, byId)} / {PFTagNameUtility.BuildFullPath(row, byId)}");
                    continue;
                }

                names[codeName] = row;
            }
        }
    }
}
