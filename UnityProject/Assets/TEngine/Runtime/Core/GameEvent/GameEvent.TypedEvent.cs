using System;

namespace TEngine
{
    /// <summary>
    /// 游戏全局事件类,类型事件拓展
    /// </summary>
    public partial class GameEvent
    {
        /// <summary>
        /// 发送类型化事件
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="eventArgs">事件参数。</param>
        public static void Send<T>(T eventArgs) where T : GameEventArgs
        {
            int eventId = GameEventArgs.GetEventId<T>();
            _eventMgr.Dispatcher.Send(eventId, eventArgs);
            MemoryPool.Release(eventArgs);
        }
        
        /// <summary>
        /// 注册类型化事件监听。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="handler">事件处理回调。</param>
        public static bool AddEventListener<T>(Action<T> handler) where T : GameEventArgs
        {
            int eventId = GameEventArgs.GetEventId<T>();
            return _eventMgr.Dispatcher.AddEventListener(eventId, handler);
        }
        
        /// <summary>
        /// 注销类型化事件监听。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="handler">事件处理回调。</param>
        public static void RemoveEventListener<T>(Action<T> handler) where T : GameEventArgs
        {
            int eventId = GameEventArgs.GetEventId<T>();
            _eventMgr.Dispatcher.RemoveEventListener(eventId, handler);
        }
    }
}