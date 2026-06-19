using System;
using System.Collections.Generic;

namespace PFGraph
{
    [Serializable]
    public sealed class Group
    {
        public long id;
        public string groupName;
        public InternalVector2Int position;
        public InternalVector2Int size;
        public InternalColor backgroundColor = new InternalColor(0.3f, 0.3f, 0.3f, 0.3f);
        public List<long> nodes = new List<long>();
    }
}