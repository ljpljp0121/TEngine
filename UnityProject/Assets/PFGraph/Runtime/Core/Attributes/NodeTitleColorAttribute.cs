using System;

namespace PFGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NodeTitleColorAttribute : Attribute
    {
        public readonly InternalColor color;

        public NodeTitleColorAttribute(float r, float g, float b)
        {
            color = new InternalColor(r, g, b, 1);
        }
    }
}
