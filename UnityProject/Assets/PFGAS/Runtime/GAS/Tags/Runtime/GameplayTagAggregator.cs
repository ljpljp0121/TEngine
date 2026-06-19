using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PFGAS;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 聚合单个 CombatUnit 的松散 Tag 和运行时来源 Tag。
    /// </summary>
    public sealed class GameplayTagAggregator
    {
        private readonly PFTagContainer looseTags = new PFTagContainer();
        private readonly Dictionary<object, HashSet<PFTagId>> sourceTags =
            new Dictionary<object, HashSet<PFTagId>>();
        private readonly Dictionary<PFTagId, int> sourceTagCounts =
            new Dictionary<PFTagId, int>();

        static GameplayTagAggregator()
        {
            RuntimeHelpers.RunClassConstructor(typeof(PFTagGenerated).TypeHandle);
        }

        public event Action TagsChanged;

        public int LooseTagCount => looseTags.Count;

        public int SourceCount => sourceTags.Count;

        public bool IsEmpty => looseTags.IsEmpty && sourceTagCounts.Count == 0;

        public bool AddLooseTag(PFTagId tagId)
        {
            var wasEffective = HasExactTag(tagId);
            if (!looseTags.AddTag(tagId))
            {
                return false;
            }

            NotifyIfEffectiveChanged(wasEffective, tagId);
            return true;
        }

        public bool AddLooseTag(int tagId)
        {
            return AddLooseTag((PFTagId)tagId);
        }

        public bool AddLooseTag(PFTag tag)
        {
            return AddLooseTag(tag.Id);
        }

        public void AddLooseTags(params PFTagId[] tags)
        {
            if (tags == null)
            {
                return;
            }

            for (var i = 0; i < tags.Length; i++)
            {
                AddLooseTag(tags[i]);
            }
        }

        public void AddLooseTags(params int[] tags)
        {
            if (tags == null)
            {
                return;
            }

            for (var i = 0; i < tags.Length; i++)
            {
                AddLooseTag(tags[i]);
            }
        }

        public bool RemoveLooseTag(PFTagId tagId)
        {
            var wasEffective = HasExactTag(tagId);
            if (!looseTags.RemoveTag(tagId))
            {
                return false;
            }

            NotifyIfEffectiveChanged(wasEffective, tagId);
            return true;
        }

        public bool RemoveLooseTag(int tagId)
        {
            return RemoveLooseTag((PFTagId)tagId);
        }

        public bool RemoveLooseTag(PFTag tag)
        {
            return RemoveLooseTag(tag.Id);
        }

        public bool AddSourceTags(object source, params PFTagId[] tags)
        {
            if (!sourceTags.TryGetValue(source, out var ownedTags))
            {
                if (tags == null || tags.Length == 0)
                {
                    return false;
                }

                ownedTags = new HashSet<PFTagId>();
                sourceTags.Add(source, ownedTags);
            }

            if (tags == null || tags.Length == 0)
            {
                return false;
            }

            var changed = false;
            for (var i = 0; i < tags.Length; i++)
            {
                var tagId = tags[i];
                if (!ownedTags.Add(tagId))
                {
                    continue;
                }

                var wasEffective = HasExactTag(tagId);
                IncrementSourceTagCount(tagId);
                NotifyIfEffectiveChanged(wasEffective, tagId);
                changed = true;
            }

            if (ownedTags.Count == 0)
            {
                sourceTags.Remove(source);
            }

            return changed;
        }

        public bool AddSourceTags(object source, params int[] tags)
        {
            return AddSourceTags(source, ToTagIds(tags));
        }

        public bool AddSourceTags(object source, params PFTag[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }

            var tagIds = new PFTagId[tags.Length];
            for (var i = 0; i < tags.Length; i++)
            {
                tagIds[i] = tags[i].Id;
            }

            return AddSourceTags(source, tagIds);
        }

        public bool RemoveSourceTags(object source)
        {
            if (!sourceTags.TryGetValue(source, out var ownedTags))
            {
                return false;
            }

            var snapshot = new PFTagId[ownedTags.Count];
            ownedTags.CopyTo(snapshot);
            sourceTags.Remove(source);

            for (var i = 0; i < snapshot.Length; i++)
            {
                var tagId = snapshot[i];
                var wasEffective = HasExactTag(tagId);
                DecrementSourceTagCount(tagId);
                NotifyIfEffectiveChanged(wasEffective, tagId);
            }

            return snapshot.Length > 0;
        }

        public bool RemoveSourceTags(object source, params PFTagId[] tags)
        {
            if (!sourceTags.TryGetValue(source, out var ownedTags) ||
                tags == null || tags.Length == 0)
            {
                return false;
            }

            var changed = false;
            for (var i = 0; i < tags.Length; i++)
            {
                var tagId = tags[i];
                if (!ownedTags.Remove(tagId))
                {
                    continue;
                }

                var wasEffective = HasExactTag(tagId);
                DecrementSourceTagCount(tagId);
                NotifyIfEffectiveChanged(wasEffective, tagId);
                changed = true;
            }

            if (ownedTags.Count == 0)
            {
                sourceTags.Remove(source);
            }

            return changed;
        }

        public bool RemoveSourceTags(object source, params int[] tags)
        {
            return RemoveSourceTags(source, ToTagIds(tags));
        }

        public bool RemoveSourceTags(object source, params PFTag[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }

            var tagIds = new PFTagId[tags.Length];
            for (var i = 0; i < tags.Length; i++)
            {
                tagIds[i] = tags[i].Id;
            }

            return RemoveSourceTags(source, tagIds);
        }

        public bool HasExactTag(PFTagId tagId)
        {
            return looseTags.HasExactTag(tagId) || sourceTagCounts.ContainsKey(tagId);
        }

        public bool HasExactTag(int tagId)
        {
            return HasExactTag((PFTagId)tagId);
        }

        public bool HasExactTag(PFTag tag)
        {
            return HasExactTag(tag.Id);
        }

        public bool HasTag(PFTagId tagId)
        {
            foreach (var tag in looseTags.Tags)
            {
                if (tag == tagId || TagHelper.IsOrUnder(tag, tagId))
                {
                    return true;
                }
            }

            foreach (var tag in sourceTagCounts.Keys)
            {
                if (tag == tagId || TagHelper.IsOrUnder(tag, tagId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasTag(int tagId)
        {
            return HasTag((PFTagId)tagId);
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

            for (var i = 0; i < tags.Length; i++)
            {
                if (!HasTag(tags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAllTags(params int[] tags)
        {
            return HasAllTags(ToTagIds(tags));
        }

        public bool HasAnyTags(params PFTagId[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < tags.Length; i++)
            {
                if (HasTag(tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyTags(params int[] tags)
        {
            return HasAnyTags(ToTagIds(tags));
        }

        public bool HasNoneTags(params PFTagId[] tags)
        {
            return !HasAnyTags(tags);
        }

        public bool HasNoneTags(params int[] tags)
        {
            return !HasAnyTags(tags);
        }

        public PFTagId[] GetLooseTagsSnapshot()
        {
            return looseTags.GetTagsSnapshot();
        }

        public PFTagId[] GetSourceTagsSnapshot()
        {
            if (sourceTagCounts.Count == 0)
            {
                return Array.Empty<PFTagId>();
            }

            var result = new PFTagId[sourceTagCounts.Count];
            sourceTagCounts.Keys.CopyTo(result, 0);
            return result;
        }

        public PFTagId[] GetTagsSnapshot()
        {
            var result = new HashSet<PFTagId>();
            foreach (var tag in looseTags.Tags)
            {
                result.Add(tag);
            }

            foreach (var tag in sourceTagCounts.Keys)
            {
                result.Add(tag);
            }

            if (result.Count == 0)
            {
                return Array.Empty<PFTagId>();
            }

            var snapshot = new PFTagId[result.Count];
            result.CopyTo(snapshot);
            return snapshot;
        }

        public void Clear()
        {
            if (IsEmpty)
            {
                return;
            }

            looseTags.Clear();
            sourceTags.Clear();
            sourceTagCounts.Clear();
            NotifyTagsChanged();
        }

        private void IncrementSourceTagCount(PFTagId tagId)
        {
            sourceTagCounts.TryGetValue(tagId, out var count);
            sourceTagCounts[tagId] = count + 1;
        }

        private void DecrementSourceTagCount(PFTagId tagId)
        {
            if (!sourceTagCounts.TryGetValue(tagId, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                sourceTagCounts.Remove(tagId);
                return;
            }

            sourceTagCounts[tagId] = count - 1;
        }

        private void NotifyIfEffectiveChanged(bool wasEffective, PFTagId tagId)
        {
            if (wasEffective != HasExactTag(tagId))
            {
                NotifyTagsChanged();
            }
        }

        private static PFTagId[] ToTagIds(int[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return Array.Empty<PFTagId>();
            }

            var tagIds = new PFTagId[tags.Length];
            for (var i = 0; i < tags.Length; i++)
            {
                tagIds[i] = (PFTagId)tags[i];
            }

            return tagIds;
        }

        private void NotifyTagsChanged()
        {
            TagsChanged?.Invoke();
        }
    }
}
