using System;

namespace PFGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeTooltipAttribute : Attribute
    {
        public readonly string Tooltip;
        
        public NodeTooltipAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
    }
}
