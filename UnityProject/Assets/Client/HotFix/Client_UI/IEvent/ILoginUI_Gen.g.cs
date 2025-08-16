// using TEngine;
//
// namespace GameLogic
// {
//     public partial class ILoginUI_Gen : ILoginUI
//     {
//         private EventDispatcher _dispatcher;
//
//         public ILoginUI_Gen(EventDispatcher dispatcher)
//         {
//             _dispatcher = dispatcher;
//             // GameEvent.EventMgr.RegWrapInterface("GameLogic.ILoginUI",this);
//             GameEvent.EventMgr.RegWrapInterface<ILoginUI>(this);
//         }
//         
//         public void ShowLoginUI()
//         {
//             _dispatcher.Send(ILoginUI_Event.ShowLoginUI);
//         }
//
//         public void CloseLoginUI()
//         {
//         }
//     }
// }