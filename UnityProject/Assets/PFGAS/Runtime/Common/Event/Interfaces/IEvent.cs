namespace PFGAS.Runtime
{
    /// <summary>
    /// 可派发的无参数事件契约。
    /// </summary>
    public interface IEvent
    {
        void Invoke();
    }
}
