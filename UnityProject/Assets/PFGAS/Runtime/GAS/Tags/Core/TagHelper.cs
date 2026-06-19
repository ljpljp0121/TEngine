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

        public static void Register(Dictionary<PFTagId, PFTag> tags)
        {
            foreach (var kv in tags)
            {
                tagMap[kv.Key] = kv.Value;
            }
        }

        public static void RegisterNames(Dictionary<PFTagId, string> names)
        {
            foreach (var kv in names)
            {
                nameMap[kv.Key] = kv.Value;
            }
        }

        public static bool IsOrUnder(int tagA, int tagB)
        {
            return IsOrUnder((PFTagId)tagA, (PFTagId)tagB);
        }

        public static bool IsOrUnder(PFTagId tagA, PFTagId tagB)
        {
            if (tagMap.TryGetValue(tagA, out var sourceTag) &&
                tagMap.TryGetValue(tagB, out var targetTag))
            {
                return sourceTag.IsOrUnder(targetTag);
            }

            return false;
        }

        public static string GetTagFullName(int tag)
        {
            return GetTagFullName((PFTagId)tag);
        }

        public static string GetTagFullName(PFTagId tag)
        {
            if (nameMap.TryGetValue(tag, out var tagName))
            {
                return tagName;
            }

            return string.Empty;
        }
    }
}
