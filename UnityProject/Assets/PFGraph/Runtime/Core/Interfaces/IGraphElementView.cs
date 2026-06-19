namespace PFGraph
{
    public interface IGraphElementView
    {
        IGraphElementProcessor V { get; }

        void Init();

        void UnInit();
    }

    public interface IGraphElementView<T> : IGraphElementView where T : IGraphElementProcessor
    {
        T ViewModel { get; }
    }
}