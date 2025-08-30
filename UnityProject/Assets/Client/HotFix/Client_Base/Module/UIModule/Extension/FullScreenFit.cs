using Client_Base;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

public class FullScreenFit : MonoBehaviour
{
    [SerializeField] 
    private bool UseScreenSize = false;
    void Start()
    {
        OnUpdateResultion();
        GameEvent.AddEventListener<E_UpdateScreenWidthOrHeight>(E_Updata_ScreenWidthOrHeight);
    }
    void OnDestroy()
    {
        GameEvent.RemoveEventListener<E_UpdateScreenWidthOrHeight>(E_Updata_ScreenWidthOrHeight);
    }

    public void E_Updata_ScreenWidthOrHeight(E_UpdateScreenWidthOrHeight e)
    {
        OnUpdateResultion();
    }

    public void OnUpdateResultion()
    {
        var rt = GetComponent<RectTransform>();
        if (TryGetComponent<Image>(out var img)) img.SetNativeSize();
        if (TryGetComponent<RawImage>(out var rawImg)) rawImg.SetNativeSize();
        rt.ForceUpdateRectTransforms();

        if (UseScreenSize)
        {
            var sx = UIModule.UIRoot.GetComponent<RectTransform>().rect.width / rt.rect.width;
            var sy = UIModule.UIRoot.GetComponent<RectTransform>().rect.height / rt.rect.height;
            //var sx = Screen.width / rt.rect.width;
            //var sy = Screen.height / rt.rect.height;
            var s = Mathf.Max(sx, sy);
            rt.sizeDelta = rt.sizeDelta * s;
        }
        else//*/
        {
            var sx = UIModule.UIRoot.GetComponent<RectTransform>().rect.width / rt.rect.width;
            var sy = UIModule.UIRoot.GetComponent<RectTransform>().rect.height / rt.rect.height;
            //Debug.Log($"{UIBehavior.WindowRoot.rect.width}*{UIBehavior.WindowRoot.rect.height} → {rt.rect.width}*{rt.rect.height}");
            //Debug.Log($"x：{sx} y：{sy} ");
            //var sx = Screen.width / rt.rect.width;
            //var sy = Screen.height / rt.rect.height;
            var s = Mathf.Max(sx, sy);
            //Debug.Log($"最终：{s}");
            rt.sizeDelta = rt.sizeDelta * s;
        }
    }
}

public class E_UpdateScreenWidthOrHeight : GameEventArgs
{
    public override void Clear()
    {
        
    }
}