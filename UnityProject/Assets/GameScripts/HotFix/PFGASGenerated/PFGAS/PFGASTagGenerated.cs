///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using GameConfig;
using PFGAS.Runtime;
using LubanTag = GameConfig.PFGAS.PFTag;
using RuntimeTag = PFGAS.Runtime.PFTag;

namespace PFGAS.Generated
{
    public static class PFGASTagGenerated
    {
        public static void RegisterFromLubanTable()
        {
            RegisterFromLubanRows(Tables.GetTable<GameConfig.PFGAS.TbPFTag>().DataList);
        }

        public static void RegisterFromLubanRows(IEnumerable<LubanTag> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            var rowList = rows.ToList();
            var byId = rowList.ToDictionary(r => r.Id);
            var childMap = rowList
                .GroupBy(r => r.ParentId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToArray());
            var tags = new Dictionary<PFTagId, RuntimeTag>();
            var names = new Dictionary<PFTagId, string>();

            foreach (var row in rowList)
            {
                var tagId = new PFTagId(row.Id);
                tags[tagId] = new RuntimeTag(
                    tagId,
                    GetParentIds(row, byId),
                    childMap.TryGetValue(row.Id, out var children)
                        ? children.Select(id => new PFTagId(id)).ToArray()
                        : Array.Empty<PFTagId>());
                names[tagId] = GetFullPath(row, byId);
            }

            TagHelper.Clear();
            TagHelper.Register(tags);
            TagHelper.RegisterNames(names);
        }

        private static PFTagId[] GetParentIds(LubanTag row, IReadOnlyDictionary<int, LubanTag> byId)
        {
            var result = new List<PFTagId>();
            var parentId = row.ParentId;
            while (parentId != -1 && byId.TryGetValue(parentId, out var parent))
            {
                result.Add(new PFTagId(parent.Id));
                parentId = parent.ParentId;
            }

            return result.ToArray();
        }

        private static string GetFullPath(LubanTag row, IReadOnlyDictionary<int, LubanTag> byId)
        {
            var segments = new List<string>();
            var current = row;
            while (current != null)
            {
                segments.Insert(0, current.Name);
                if (current.ParentId == -1 || !byId.TryGetValue(current.ParentId, out current))
                {
                    break;
                }
            }

            return string.Join(".", segments);
        }
    }
}
