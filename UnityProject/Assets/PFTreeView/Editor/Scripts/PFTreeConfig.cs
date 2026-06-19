using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFTreeView
{
    [Serializable]
    public class PFTreeNodeConfig
    {
        public int Id;
        public string Name;
        public int Depth;
        public int ParentId; //父节点,根节点的父节点为-1
    }

    public class PFTreeConfig<T> : ScriptableObject where T : PFTreeNodeConfig
    {
        [Tooltip("生成代码输出目录，例如 Assets/Scripts/Generated")]
        public string CodeGenPath = "";
        public List<T> Nodes = new();
    }
}
