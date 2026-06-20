using System.Collections.Generic;

namespace PFGAS.Editor
{
    public sealed class PFTagTreeNode
    {
        private readonly List<PFTagTreeNode> children = new List<PFTagTreeNode>();

        public PFTagTreeNode(int id, string name, int depth)
        {
            ID = id;
            Name = name;
            Depth = depth;
            Data = new PFTagNodeConfig
            {
                Id = id,
                Name = name,
                Depth = depth,
            };
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public int Depth { get; set; }
        public PFTagNodeConfig Data { get; set; }
        public PFTagTreeNode Parent { get; set; }
        public List<PFTagTreeNode> Children => children;
        public bool HasChildren => children.Count > 0;

        public void AddChild(PFTagTreeNode child)
        {
            children.Add(child);
            child.Parent = this;
            child.Depth = Depth + 1;
            child.SyncDataFromNode();
        }

        public void InsertChild(int index, PFTagTreeNode child)
        {
            children.Insert(index, child);
            child.Parent = this;
            child.Depth = Depth + 1;
            child.SyncDataFromNode();
        }

        public void RemoveChild(PFTagTreeNode child)
        {
            if (children.Remove(child))
            {
                child.Parent = null;
            }
        }

        public bool IsChildOf(PFTagTreeNode node)
        {
            var current = Parent;
            while (current != null)
            {
                if (current == node)
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        public void SyncDataFromNode()
        {
            Data ??= new PFTagNodeConfig();
            Data.Id = ID;
            Data.Name = Name;
            Data.Depth = Depth;
            Data.ParentId = Parent?.ID ?? PFTagExcelRow.RootParentId;
        }
    }
}
