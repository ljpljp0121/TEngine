using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>一组可以一起挂到单位上的属性规则；处理器依赖必须落在同一个 AttributeSet 内。</summary>
    public sealed class AttributeSet
    {
        private readonly AttributeSetEntry[] entries;

        public AttributeSet(int id, string name, IEnumerable<AttributeSetEntry> entries)
        {
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? id.ToString() : name;
            this.entries = CopyAndValidateEntries(entries);
        }

        public int Id { get; }

        public string Name { get; }

        public IReadOnlyList<AttributeSetEntry> Entries => entries;

        private static AttributeSetEntry[] CopyAndValidateEntries(IEnumerable<AttributeSetEntry> entries)
        {
            if (entries == null)
            {
                GASGuard.ThrowArgument("AttributeSet entries cannot be null.", nameof(entries));
            }

            var copiedEntries = new List<AttributeSetEntry>();
            var entryIds = new HashSet<PFAttributeId>();
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    GASGuard.ThrowArgument("AttributeSet cannot contain a null entry.");
                }

                if (!entryIds.Add(entry.Id))
                {
                    GASGuard.ThrowInvalidOperation(
                        $"AttributeSet contains duplicated attribute '{entry.Id}'.");
                }

                copiedEntries.Add(entry);
            }

            if (copiedEntries.Count == 0)
            {
                GASGuard.ThrowArgument("AttributeSet must contain at least one attribute.");
            }

            ValidateDependenciesStayInsideSet(copiedEntries, entryIds);
            ValidateNoDependencyCycles(copiedEntries);
            return copiedEntries.ToArray();
        }

        private static void ValidateDependenciesStayInsideSet(
            IReadOnlyList<AttributeSetEntry> entries,
            HashSet<PFAttributeId> entryIds)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                for (var dependencyIndex = 0;
                     dependencyIndex < entry.RequiredAttributes.Count;
                     dependencyIndex++)
                {
                    var dependencyId = entry.RequiredAttributes[dependencyIndex];
                    if (!entryIds.Contains(dependencyId))
                    {
                        GASGuard.ThrowInvalidOperation(
                            $"AttributeSet entry '{entry.Id}' depends on attribute '{dependencyId}' outside the set.");
                    }
                }
            }
        }

        private static void ValidateNoDependencyCycles(IReadOnlyList<AttributeSetEntry> entries)
        {
            var entryMap = new Dictionary<PFAttributeId, AttributeSetEntry>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
            {
                entryMap.Add(entries[i].Id, entries[i]);
            }

            var visitStates = new Dictionary<PFAttributeId, int>();
            for (var i = 0; i < entries.Count; i++)
            {
                VisitEntry(entries[i].Id, entryMap, visitStates);
            }
        }

        private static void VisitEntry(
            PFAttributeId attributeId,
            Dictionary<PFAttributeId, AttributeSetEntry> entries,
            Dictionary<PFAttributeId, int> visitStates)
        {
            if (visitStates.TryGetValue(attributeId, out var state))
            {
                if (state == 1)
                {
                    GASGuard.ThrowInvalidOperation(
                        "AttributeSet contains a dependency cycle.");
                }

                return;
            }

            var entry = entries[attributeId];
            visitStates.Add(attributeId, 1);
            for (var i = 0; i < entry.RequiredAttributes.Count; i++)
            {
                VisitEntry(entry.RequiredAttributes[i], entries, visitStates);
            }

            visitStates[attributeId] = 2;
        }
    }
}
