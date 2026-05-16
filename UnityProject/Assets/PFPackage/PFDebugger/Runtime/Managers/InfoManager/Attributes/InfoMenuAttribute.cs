using System;

namespace PFDebugger
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class InfoMenuAttribute : Attribute
    {
        public string Path;
        public int Order; //Order越小,按钮越靠后

        public InfoMenuAttribute(string path, int order)
        {
            Path = path;
            Order = order;
        }
    }
}