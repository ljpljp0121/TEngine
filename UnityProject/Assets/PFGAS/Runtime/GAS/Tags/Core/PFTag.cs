using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// Represents one generated gameplay tag and its hierarchy links.
    /// </summary>
    public readonly struct PFTag
    {
        public readonly PFTagId Id;
        public readonly PFTagId[] Parents;
        public readonly PFTagId[] Children;

        public PFTag(PFTagId tagId, PFTagId[] parents, PFTagId[] children)
        {
            Id = tagId;
            Parents = parents ?? Array.Empty<PFTagId>();
            Children = children ?? Array.Empty<PFTagId>();
        }

        public bool IsOrUnder(PFTagId tagId)
        {
            if (Id == tagId)
            {
                return true;
            }

            foreach (var parent in Parents)
            {
                if (parent == tagId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsParentOf(PFTagId tagId)
        {
            foreach (var child in Children)
            {
                if (child == tagId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsChildOf(PFTagId tagId)
        {
            foreach (var parent in Parents)
            {
                if (parent == tagId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsOrUnder(PFTag tag)
        {
            return IsOrUnder(tag.Id);
        }

        public bool IsParentOf(PFTag tag)
        {
            return IsParentOf(tag.Id);
        }

        public bool IsChildOf(PFTag tag)
        {
            return IsChildOf(tag.Id);
        }

        public static bool operator ==(PFTag x, PFTag y)
        {
            return x.Id == y.Id;
        }

        public static bool operator !=(PFTag x, PFTag y)
        {
            return x.Id != y.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is PFTag other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool IsRoot => Parents.Length == 0;

        public bool HasChild => Children.Length > 0;
    }
}
