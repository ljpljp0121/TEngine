using System;

namespace PFDebugger
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
    public class DebuggerTabAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; }

        public DebuggerTabAttribute(string name, int order = 0)
        {
            Name = name;
            Order = order;
        }
    }
}
