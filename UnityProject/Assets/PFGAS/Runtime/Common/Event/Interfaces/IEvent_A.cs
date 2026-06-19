namespace PFGAS.Runtime
{
    /// <summary>
    /// 可派发的单参数事件契约。
    /// </summary>
    public interface IEvent<T>
    {
        void Invoke(in T arg);
    }
}
