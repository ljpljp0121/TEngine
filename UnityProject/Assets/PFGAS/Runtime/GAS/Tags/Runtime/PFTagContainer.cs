using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>
    /// Runtime tag container backed by generated PFTag ids.
    /// </summary>
    public sealed class PFTagContainer
    {
        private readonly HashSet<PFTagId> tagIds = new HashSet<PFTagId>();

        public PFTagContainer()
        {
        }

        public PFTagContainer(params PFTagId[] tags)
        {
            AddTags(tags);
        }

        public PFTagContainer(params int[] tags)
        {
            AddTags(tags);
        }

        public int Count => tagIds.Count;

        public bool IsEmpty => tagIds.Count == 0;

        public IEnumerable<PFTagId> Tags => GetTagsSnapshot();

        public bool AddTag(PFTagId tagId)
        {
            return tagIds.Add(tagId);
        }

        public bool AddTag(int tagId)
        {
            return AddTag(new PFTagId(tagId));
        }

        public bool AddTag(PFTag tag)
        {
            return AddTag(tag.Id);
        }

        public void AddTags(params PFTagId[] tags)
        {
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                AddTag(tag);
            }
        }

        public void AddTags(params int[] tags)
        {
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                AddTag(tag);
            }
        }

        public bool RemoveTag(PFTagId tagId)
        {
            return tagIds.Remove(tagId);
        }

        public bool RemoveTag(int tagId)
        {
            return RemoveTag(new PFTagId(tagId));
        }

        public bool RemoveTag(PFTag tag)
        {
            return RemoveTag(tag.Id);
        }

        public bool HasExactTag(PFTagId tagId)
        {
            return tagIds.Contains(tagId);
        }

        public bool HasExactTag(int tagId)
        {
            return HasExactTag(new PFTagId(tagId));
        }

        public bool HasExactTag(PFTag tag)
        {
            return HasExactTag(tag.Id);
        }

        public bool HasTag(PFTagId tagId)
        {
            foreach (var ownedTag in tagIds)
            {
                if (ownedTag == tagId || TagHelper.IsOrUnder(ownedTag, tagId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasTag(int tagId)
        {
            return HasTag(new PFTagId(tagId));
        }

        public bool HasTag(PFTag tag)
        {
            return HasTag(tag.Id);
        }

        public bool HasAllTags(params PFTagId[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return true;
            }

            foreach (var tag in tags)
            {
                if (!HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAllTags(params int[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return true;
            }

            foreach (var tag in tags)
            {
                if (!HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAnyTags(params PFTagId[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyTags(params int[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasNoneTags(params PFTagId[] tags)
        {
            return !HasAnyTags(tags);
        }

        public bool HasNoneTags(params int[] tags)
        {
            return !HasAnyTags(tags);
        }

        public void Clear()
        {
            tagIds.Clear();
        }

        public PFTagId[] GetTagsSnapshot()
        {
            if (tagIds.Count == 0)
            {
                return Array.Empty<PFTagId>();
            }

            var result = new PFTagId[tagIds.Count];
            tagIds.CopyTo(result);
            return result;
        }

        public int[] GetTagIdsSnapshot()
        {
            if (tagIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            var result = new int[tagIds.Count];
            var index = 0;
            foreach (var tag in tagIds)
            {
                result[index++] = (int)tag;
            }

            return result;
        }
    }
}
