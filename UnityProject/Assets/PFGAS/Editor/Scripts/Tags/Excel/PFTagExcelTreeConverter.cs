using System.Collections.Generic;
using System.Linq;

namespace PFGAS.Editor
{
    public static class PFTagExcelTreeConverter
    {
        public static List<PFTagNodeConfig> ToNodeConfigs(IReadOnlyList<PFTagExcelRow> rows)
        {
            var byId = rows.ToDictionary(r => r.Id, r => r);
            var children = rows
                .GroupBy(r => r.ParentId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var result = new List<PFTagNodeConfig>();

            if (children.TryGetValue(PFTagExcelRow.RootParentId, out var roots))
            {
                foreach (var root in roots)
                {
                    AddPreOrder(root, 0);
                }
            }

            foreach (var row in rows)
            {
                if (!result.Any(n => n.Id == row.Id))
                {
                    AddPreOrder(row, CalculateDepth(row, byId));
                }
            }

            return result;

            void AddPreOrder(PFTagExcelRow row, int depth)
            {
                result.Add(new PFTagNodeConfig
                {
                    Id = row.Id,
                    ParentId = row.ParentId,
                    Name = row.Name,
                    Depth = depth,
                    Desc = row.Desc,
                    FullPath = row.FullPath,
                });

                if (!children.TryGetValue(row.Id, out var childRows))
                {
                    return;
                }

                foreach (var child in childRows)
                {
                    AddPreOrder(child, depth + 1);
                }
            }
        }

        public static List<PFTagExcelRow> FromTree(PFTagTreeModel treeModel)
        {
            var rows = new List<PFTagExcelRow>();
            foreach (var node in treeModel.GetData())
            {
                if (node.Depth < 0)
                {
                    continue;
                }

                rows.Add(new PFTagExcelRow
                {
                    Id = node.ID,
                    ParentId = node.Parent?.ID ?? PFTagExcelRow.RootParentId,
                    Name = node.Name,
                    Desc = node.Data?.Desc ?? string.Empty,
                });
            }

            var byId = rows.ToDictionary(r => r.Id, r => r);
            foreach (var row in rows)
            {
                row.FullPath = PFTagNameUtility.BuildFullPath(row, byId);
            }

            return rows;
        }

        private static int CalculateDepth(PFTagExcelRow row, IReadOnlyDictionary<int, PFTagExcelRow> byId)
        {
            var depth = 0;
            var seen = new HashSet<int> { row.Id };
            var parentId = row.ParentId;
            while (parentId != PFTagExcelRow.RootParentId &&
                   byId.TryGetValue(parentId, out var parent) &&
                   seen.Add(parent.Id))
            {
                depth++;
                parentId = parent.ParentId;
            }

            return depth;
        }
    }
}
