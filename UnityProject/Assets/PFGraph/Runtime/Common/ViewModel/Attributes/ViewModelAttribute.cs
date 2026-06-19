using System;

namespace PFGraph
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewModelAttribute : Attribute
    {
        public Type ModelType;

        public ViewModelAttribute(Type modelType)
        {
            this.ModelType = modelType;
        }
    }
}
