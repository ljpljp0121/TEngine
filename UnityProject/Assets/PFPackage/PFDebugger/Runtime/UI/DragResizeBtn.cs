using UnityEngine;
using UnityEngine.EventSystems;

namespace PFDebugger
{
    public class DragResizeBtn : MonoBehaviour, IBeginDragHandler,IDragHandler
    {
        public void OnBeginDrag(PointerEventData eventData)
        {
            Debugger.DebuggerMainWindow.BeginResize(eventData);
        }
        public void OnDrag(PointerEventData eventData)
        {
            Debugger.DebuggerMainWindow.Resize(eventData);
        }
    }
}
