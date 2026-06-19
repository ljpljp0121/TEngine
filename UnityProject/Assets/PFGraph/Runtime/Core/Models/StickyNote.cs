using System;

namespace PFGraph
{
    [Serializable]
    public sealed class StickyNote
    {
        public long id;
        public InternalVector2Int position;
        public InternalVector2Int size;
        public string title;
        public string content;
    }
}