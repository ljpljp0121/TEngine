using System;

namespace PFDebugger
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class InfoItemAttribute : Attribute
    {
        public string DisplayName;

        public InfoItemAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
