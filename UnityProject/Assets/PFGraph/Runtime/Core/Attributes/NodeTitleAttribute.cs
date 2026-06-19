using System;

namespace PFGraph
{
    /// <summary> 节点标题 </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NodeTitleAttribute : Attribute
    {
        /// <summary> 节点标题名称 </summary>
        public string title;

        public NodeTitleAttribute(string title)
        {
            this.title = title;
        }
    }
}