using System.Collections;
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
                if (isActive) OnShow();
                else OnHide();
            }
        }

        [SerializeField] private DebuggerText InfoCountText;
        [SerializeField] private DebuggerText WarningCountText;
        [SerializeField] private DebuggerText ErrorCountText;
        [SerializeField] private DebuggerText FrameCountText;

        private Image backgroundImage;
        private RectTransform rectTransform;
        private bool isBeginDrag;
        private Vector2 normalizedPosition;
        private Vector2 halfSize;
        private IEnumerator moveToPosCoroutine = null;

        public void OnInit()
        {
            backgroundImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            halfSize = rectTransform.sizeDelta * 0.5f;

            Vector2 pos = rectTransform.anchoredPosition;
            if (pos.x != 0f || pos.y != 0f)
                normalizedPosition = pos.normalized;
            else
                normalizedPosition = new Vector2(0.5f, 0f);
            
            OnLogChanged();
        }

        public void OnDeInit() { }

        private float lastFps = 0;
        
        public void Tick()
        {
            if (!Mathf.Approximately(lastFps, Debugger.FPSCounterManager.CurrentFps))
            {
                FrameCountText.text = Debugger.FPSCounterManager.CurrentFps.ToString("F1");
                lastFps = Debugger.FPSCounterManager.CurrentFps;
            }
        }

        private void OnShow()
        {
            UpdatePosition(true);
            Debugger.LogManager.OnLogChanged += OnLogChanged;
        }

        private void OnHide()
        {
            isBeginDrag = false;
            Debugger.LogManager.OnLogChanged -= OnLogChanged;
        }

        private void OnLogChanged()
        {
            InfoCountText.text = Debugger.LogManager.InfoCount.ToString();
            WarningCountText.text = Debugger.LogManager.WarningCount.ToString();
            ErrorCountText.text = Debugger.LogManager.ErrorCount.ToString();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isBeginDrag)
            {
                Debugger.VisibleMainWindow();
            }
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
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(DebuggerManager.I.RectTransform,
                    eventData.position, eventData.pressEventCamera, out localPoint))
                rectTransform.anchoredPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isBeginDrag = false;
            UpdatePosition(false);
        }

        /// <summary> 更新位置 ，如果不立即更新，会有一个简单动画 </summary>
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

            while (modifier < 1f)
            {
                modifier += 4f * Time.unscaledDeltaTime;
                rectTransform.anchoredPosition = Vector2.Lerp(initialPos, targetPos, modifier);

                yield return null;
            }
        }
    }
}