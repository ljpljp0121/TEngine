namespace PFGraph
{
    public interface IEvent<T>
    {
        void Invoke(in T arg);
    }
}