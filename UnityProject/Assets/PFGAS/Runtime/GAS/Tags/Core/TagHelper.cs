using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>
    /// Stores generated PFTag registration data and hierarchy queries.
    /// </summary>
    public static class TagHelper
    {
        private static readonly Dictionary<PFTagId, PFTag> tagMap = new Dictionary<PFTagId, PFTag>();
        private static readonly Dictionary<PFTagId, string> nameMap = new Dictionary<PFTagId, string>();

        public static bool IsRegistered { get; private set; }

        public static void Clear()
        {
            tagMap.Clear();
            nameMap.Clear();
            IsRegistered = false;
        }

        public static void Register(Dictionary<PFTagId, PFTag> tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            foreach (var kv in tags)
            {
                tagMap[kv.Key] = kv.Value;
            }

            IsRegistered = tagMap.Count > 0;
        }

        public static void RegisterNames(Dictionary<PFTagId, string> names)
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            foreach (var kv in names)
            {
                nameMap[kv.Key] = kv.Value;
            }
        }

        public static bool IsOrUnder(int tagA, int tagB)
        {
            return IsOrUnder(new PFTagId(tagA), new PFTagId(tagB));
        }

        public static bool IsOrUnder(PFTagId tagA, PFTagId tagB)
        {
            EnsureRegistered();

            if (tagMap.TryGetValue(tagA, out var sourceTag) &&
                tagMap.TryGetValue(tagB, out var targetTag))
            {
                return sourceTag.IsOrUnder(targetTag);
            }

            return false;
        }

        public static string GetTagFullName(int tag)
        {
            return GetTagFullName(new PFTagId(tag));
        }

        public static string GetTagFullName(PFTagId tag)
        {
            EnsureRegistered();

            if (nameMap.TryGetValue(tag, out var tagName))
            {
                return tagName;
            }

            return string.Empty;
        }

        private static void EnsureRegistered()
        {
            if (!IsRegistered)
            {
                throw new InvalidOperationException(
                    "PFGAS Tag data has not been registered. Call the generated PFGAS tag adapter during startup or test bootstrap.");
            }
        }
    }
}
