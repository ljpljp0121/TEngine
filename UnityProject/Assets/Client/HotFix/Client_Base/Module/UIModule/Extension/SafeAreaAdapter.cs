using UnityEngine;
using TEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdapter : MonoBehaviour
{
    [Header("适配设置")]
    [SerializeField]
    private bool adaptTop = true;
    [SerializeField]
    private bool adaptBottom = true;
    [SerializeField]
    private bool adaptLeft = false;
    [SerializeField]
    private bool adaptRight = false;
    
    [Header("手动调节")]
    [SerializeField, Range(-100f, 100f)]
    private float extraOffset = 0f; // 额外偏移像素 (负数=更靠近边缘，正数=更远离边缘)

    private RectTransform rectTransform;
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        ApplySafeArea();
        GameEvent.AddEventListener<E_UpdateScreenWidthOrHeight>(OnScreenSizeChanged);
    }
    
    void OnDestroy()
    {
        GameEvent.RemoveEventListener<E_UpdateScreenWidthOrHeight>(OnScreenSizeChanged);
    }
    
    void OnScreenSizeChanged(E_UpdateScreenWidthOrHeight e)
    {
        ApplySafeArea();
    }
    
    // Editor下实时预览
    void OnValidate()
    {
        if (Application.isPlaying && rectTransform != null)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
        }
        
        Rect safeArea = Screen.safeArea;
        
        // 应用额外偏移
        safeArea.x += extraOffset;
        safeArea.y += extraOffset;  
        safeArea.width -= extraOffset * 2;
        safeArea.height -= extraOffset * 2;
        
        // 确保不超出屏幕范围
        safeArea.x = Mathf.Max(0, safeArea.x);
        safeArea.y = Mathf.Max(0, safeArea.y);
        safeArea.width = Mathf.Min(Screen.width - safeArea.x, safeArea.width);
        safeArea.height = Mathf.Min(Screen.height - safeArea.y, safeArea.height);
        
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        Vector2 currentAnchorMin = rectTransform.anchorMin;
        Vector2 currentAnchorMax = rectTransform.anchorMax;

        if (adaptLeft) currentAnchorMin.x = anchorMin.x;
        if (adaptBottom) currentAnchorMin.y = anchorMin.y;
        if (adaptRight) currentAnchorMax.x = anchorMax.x;
        if (adaptTop) currentAnchorMax.y = anchorMax.y;
        
        rectTransform.anchorMin = currentAnchorMin;
        rectTransform.anchorMax = currentAnchorMax;
    }
}