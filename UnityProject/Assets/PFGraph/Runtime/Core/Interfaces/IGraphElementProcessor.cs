using System;

namespace PFGraph
{
    public interface IGraphElementProcessor
    {
        object Model { get; }

        Type ModelType { get; }
    }

    public interface IGraphElementProcessor<T> : IGraphElementProcessor { }

    public interface IGraphElementProcessor_Scope
    {
        public InternalVector2Int Position { get; set; }
    }
}