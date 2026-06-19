using System;

namespace PFGraph
{
    /// <summary> 节点菜单，和自定义节点名 </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NodeMenuAttribute : Attribute
    {
        /// <summary> 节点路径 </summary>
        public string path;
        /// <summary> 是否要显示在节点菜单中 </summary>
        public bool hidden;

        public NodeMenuAttribute(string path)
        {
            this.path = path;
        }
    }
}