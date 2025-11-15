#if DOTWEEN
using DG.Tweening;
#endif
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SuperListCell : MonoBehaviour, IPointerClickHandler
{
    protected Action<SuperListCell> cellClick = null;

    protected object _data;
    protected bool selected;

    [HideInInspector]
    public CanvasGroup canvasGroup;
    [HideInInspector]
    public int index;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnPointerClick();
        cellClick?.Invoke(this);
    }

    public virtual void OnPointerClick() { }

    public virtual void UpdateData(object _data)
    {
        this._data = _data;
    }

    public virtual void SetSelected(bool value)
    {
        selected = value;
    }

    public virtual void SetData(object _data)
    {
        this._data = _data;
    }

    public bool TrySetData(object _data)
    {
        bool ret = false;
        try
        {
            SetData(_data);
            ret = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{e.Message}\n{e.StackTrace}");
        }

        return ret;
    }

    public void SetClickHandle(Action<SuperListCell> cellClick)
    {
        this.cellClick = cellClick;
    }


    #region 渐入渐出处理

#if DOTWEEN
    private Tweener tween;
#else
    private Coroutine alphaCoroutine;
#endif

    public int AlphaIn(Action callback)
    {
#if DOTWEEN
        void Dele()
        {
            tween = null;
            callback?.Invoke();
        }

        tween = DOVirtual.Float(0, 1, 0.1f, AlphaInDel);
        tween.OnComplete(Dele);
#else
        alphaCoroutine = StartCoroutine(AlphaInCoroutine(0.1f, callback));
#endif
        return gameObject.GetInstanceID();
    }

    public void StopAlphaIn()
    {
#if DOTWEEN
        if (tween != null)
        {
            tween.Kill();
            tween = null;
        }
#else
        if (alphaCoroutine != null)
        {
            StopCoroutine(alphaCoroutine);
            alphaCoroutine = null;
        }
#endif
    }

    public void ReturnInit()
    {
        StopAlphaIn();
        AlphaInDel(1);
    }

    private void AlphaInDel(float value)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = value;
    }

#if !DOTWEEN
    private System.Collections.IEnumerator AlphaInCoroutine(float duration, Action callback)
    {
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            AlphaInDel(currentAlpha);
            yield return null;
        }

        AlphaInDel(targetAlpha);
        alphaCoroutine = null;
        callback?.Invoke();
    }
#endif

    #endregion
    
}