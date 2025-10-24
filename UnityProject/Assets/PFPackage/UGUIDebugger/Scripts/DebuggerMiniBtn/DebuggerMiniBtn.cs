using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("alertColorInfo")]
        [SerializeField]
        private Color AlertColorInfo;
        [FormerlySerializedAs("alertColorWarning")]
        [SerializeField]
        private Color AlertColorWarning;
        [FormerlySerializedAs("alertColorError")]
        [SerializeField]
        private Color AlertColorError;

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
            RectTransform managerRect = DebuggerManager.I.RectTransform;
            Vector2 canvasSize = managerRect.rect.size;
            float canvasWidth = canvasSize.x;
            float canvasHeight = canvasSize.y;
            
            // 计算当前位置
            Vector2 currentPosition;
            if (immediately)
            {
                currentPosition = canvasSize * 0.5f + new Vector2(
                    normalizedPosition.x * canvasWidth, 
                    normalizedPosition.y * canvasHeight);
            }
            else
            {
                currentPosition = rectTransform.anchoredPosition + canvasSize * 0.5f;
            }
            
            float distanceToLeft = currentPosition.x;
            float distanceToRight = canvasWidth - distanceToLeft;
            float distanceToBottom = currentPosition.y;
            float distanceToTop = canvasHeight - distanceToBottom;
            
            // 确定最近的边缘方向
            Vector2 targetPosition;
            if (Mathf.Min(distanceToLeft, distanceToRight) < Mathf.Min(distanceToBottom, distanceToTop))
            {
                // 水平边缘更近
                if (distanceToLeft < distanceToRight)
                {
                    targetPosition = new Vector2(halfSize.x, currentPosition.y);
                }
                else
                {
                    targetPosition = new Vector2(canvasWidth - halfSize.x, currentPosition.y);
                }
        
                // 限制Y轴范围
                targetPosition.y = Mathf.Clamp(targetPosition.y, halfSize.y, canvasHeight - halfSize.y);
            }
            else
            {
                // 垂直边缘更近
                if (distanceToBottom < distanceToTop)
                {
                    targetPosition = new Vector2(currentPosition.x, halfSize.y);
                }
                else
                {
                    targetPosition = new Vector2(currentPosition.x, canvasHeight - halfSize.y);
                }
        
                // 限制X轴范围
                targetPosition.x = Mathf.Clamp(targetPosition.x, halfSize.x, canvasWidth - halfSize.x);
            }
            
            normalizedPosition.Set(
                (targetPosition.x - canvasSize.x * 0.5f) / canvasWidth,
                (targetPosition.y - canvasSize.y * 0.5f) / canvasHeight);
            
            Vector2 finalPosition = targetPosition - canvasSize * 0.5f;
            
            if (moveToPosCoroutine != null)
            {
                StopCoroutine(moveToPosCoroutine);
                moveToPosCoroutine = null;
            }
            
            if (immediately)
            {
                rectTransform.anchoredPosition = finalPosition;
            }
            else
            {
                moveToPosCoroutine = MoveToPosAnimation(finalPosition);
                StartCoroutine(moveToPosCoroutine);
            }
        }
        
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