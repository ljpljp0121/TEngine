using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PFDebugger
{
    public class DebuggerMiniBtn : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private bool isActive;
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                gameObject.SetActive(isActive);
                OnVisible(isActive);
            }
        }

        [SerializeField]
        private Text InfoCountText;
        [SerializeField]
        private Text WarningCountText;
        [SerializeField]
        private Text ErrorCountText;
        [SerializeField]
        private Text FrameCountText;
        [SerializeField]
        private Color alertColorInfo;
        [SerializeField]
        private Color alertColorWarning;
        [SerializeField]
        private Color alertColorError;

        private Image backgroundImage;
        private RectTransform rectTransform;

        private FPSCounter fpsCounter;
        private bool isInit;
        private Color normalColor;
        private bool isBeginDrag;
        private Vector2 normalizedPosition;
        private Vector2 halfSize;
        private IEnumerator moveToPosCoroutine = null;

        internal void OnAwake()
        {
            fpsCounter = new FPSCounter(0.5f);
            backgroundImage = GetComponent<Image>();
            normalColor = backgroundImage.color;
            rectTransform = GetComponent<RectTransform>();
            halfSize = rectTransform.sizeDelta * 0.5f; 
            
            Vector2 pos = rectTransform.anchoredPosition;
            if (pos.x != 0f || pos.y != 0f)
                normalizedPosition = pos.normalized;
            else
                normalizedPosition = new Vector2(0.5f, 0f);
        }

        internal void OnUpdate()
        {
            fpsCounter.Update(Time.deltaTime, Time.unscaledDeltaTime);
            FrameCountText.text = fpsCounter.CurrentFps.ToString("F2");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isBeginDrag = true;
            if (moveToPosCoroutine != null)
            {
                StopCoroutine(moveToPosCoroutine);
                moveToPosCoroutine = null;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(DebuggerManager.I.RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
                rectTransform.anchoredPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isBeginDrag = false;
            UpdatePosition(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(!isBeginDrag)
                DebuggerManager.I.ShowMainWindow(true);
        }   

        /// <summary>
        /// 更新位置 ，如果不立即更新，会有一个简单动画
        /// </summary>
        /// <param name="immediately"></param>
        private void UpdatePosition(bool immediately)
        {
            Vector2 canvasRawSize = DebuggerManager.I.RectTransform.rect.size;
            Debug.Log($"Canvas size: {canvasRawSize}, anchoredPosition: {rectTransform.anchoredPosition}");
            float canvasWidth = canvasRawSize.x;
            float canvasHeight = canvasRawSize.y;

            float canvasBottomLeftX = 0f;
            float canvasBottomLeftY = 0f;

            // Calculate safe area position of the popup
            // normalizedPosition allows us to glue the popup to a specific edge of the screen. It becomes useful when
            // the popup is at the right edge and we switch from portrait screen orientation to landscape screen orientation.
            // Without normalizedPosition, popup could jump to bottom or top edges instead of staying at the right edge
            Vector2 pos = canvasRawSize * 0.5f + (immediately
                ? new Vector2(normalizedPosition.x * canvasWidth, normalizedPosition.y * canvasHeight)
                : (rectTransform.anchoredPosition - new Vector2(canvasBottomLeftX, canvasBottomLeftY)));
            
            // Find distances to all four edges of the safe area
            float distToLeft = pos.x;
            float distToRight = canvasWidth - distToLeft;

            float distToBottom = pos.y;
            float distToTop = canvasHeight - distToBottom;
            
            float horDistance = Mathf.Min( distToLeft, distToRight );
            float vertDistance = Mathf.Min( distToBottom, distToTop );
            
            if( horDistance < vertDistance )
            {
                if( distToLeft < distToRight )
                    pos = new Vector2( halfSize.x, pos.y );
                else
                    pos = new Vector2( canvasWidth - halfSize.x, pos.y );

                pos.y = Mathf.Clamp( pos.y, halfSize.y, canvasHeight - halfSize.y );
            }
            else
            {
                if( distToBottom < distToTop )
                    pos = new Vector2( pos.x, halfSize.y );
                else
                    pos = new Vector2( pos.x, canvasHeight - halfSize.y );

                pos.x = Mathf.Clamp( pos.x, halfSize.x, canvasWidth - halfSize.x );
            }
            
            pos -= canvasRawSize * 0.5f;
            
            normalizedPosition.Set( pos.x / canvasWidth, pos.y / canvasHeight );
            
            // Safe area's bottom left coordinates are added to pos only after normalizedPosition's value
            // is set because normalizedPosition is in range [-canvasWidth / 2, canvasWidth / 2]
            pos += new Vector2( canvasBottomLeftX, canvasBottomLeftY );
            
            // If another smooth movement animation is in progress, cancel it
            if( moveToPosCoroutine != null )
            {
                StopCoroutine( moveToPosCoroutine );
                moveToPosCoroutine = null;
            }
            
            if( immediately )
                rectTransform.anchoredPosition = pos;
            else
            {
                // Smoothly translate the popup to the specified position
                moveToPosCoroutine = MoveToPosAnimation( pos );
                StartCoroutine( moveToPosCoroutine );
            }
        }
    
        // A simple smooth movement animation
        private IEnumerator MoveToPosAnimation(Vector2 targetPos)
        {
            float modifier = 0f;
            Vector2 initialPos = rectTransform.anchoredPosition;

            while( modifier < 1f )
            {
                modifier += 4f * Time.unscaledDeltaTime;
                rectTransform.anchoredPosition = Vector2.Lerp( initialPos, targetPos, modifier );

                yield return null;
            }
        }

        /// <summary>
        /// 显示隐藏时调用
        /// </summary>
        /// <param name="isShow"></param>
        private void OnVisible(bool isShow)
        {
            if (isShow)
            {
                UpdatePosition( true );
            }
            else
            {
                isBeginDrag = false;
            }
        }
    }
}