using System.Collections.Generic;

namespace PFDebugger
{
    public class InfoPathNode
    {
        public string Name;
        public string Path;
        public int Order;
        public InfoPathNodeType NodeType;
        public InfoBase Info;
        public List<InfoPathNode> Children = new List<InfoPathNode>();
        public InfoPathNode Parent;

        public bool IsLeaf => Children == null || Children.Count == 0;
    }
}