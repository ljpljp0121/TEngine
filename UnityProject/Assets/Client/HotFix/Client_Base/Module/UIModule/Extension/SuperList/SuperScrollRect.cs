using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SuperScrollRect : ScrollRect, IPointerExitHandler
{
    public bool isRestrain = false;
    private bool isRestrainDrag;
    private bool isOneTouchDrag;

    public static bool CanDrag
    {
        get => SuperScrollRectScript.Instance.canDrag;
        set => SuperScrollRectScript.Instance.canDrag = value;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag || Input.touchCount > 1)
            return;
        if (isRestrain)
            isRestrainDrag = true;
        isOneTouchDrag = true;
        base.OnBeginDrag(eventData);
        GameEvent.Send(E_OnSuperListBeginDrag.Create(this.gameObject));
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        GameEvent.Send(E_OnSuperListEndDrag.Create(this.gameObject));
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (!CanDrag)
            return;
        if (Input.touchCount > 1)
        {
            isOneTouchDrag = false;
            return;
        }

        if (isOneTouchDrag && (!isRestrain || isRestrainDrag))
            base.OnDrag(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isRestrain)
        {
            base.OnEndDrag(eventData);
            isRestrainDrag = false;
        }
    }
}

public class E_OnSuperListBeginDrag : GameEventArgs
{
    public GameObject owner;
    
    public static E_OnSuperListBeginDrag Create(GameObject owner)
    {
        E_OnSuperListBeginDrag data = MemoryPool.Acquire<E_OnSuperListBeginDrag>();
        data.owner = owner;
        return data;
    }
    
    
    public override void Clear()
    {
        owner = null;
    }
}

public class E_OnSuperListEndDrag : GameEventArgs
{
    public GameObject owner;
 
    public static E_OnSuperListEndDrag Create(GameObject owner)
    {
        E_OnSuperListEndDrag data = MemoryPool.Acquire<E_OnSuperListEndDrag>();
        data.owner = owner;
        return data;
    }

    public override void Clear()
    {
        owner = null;
    }
}