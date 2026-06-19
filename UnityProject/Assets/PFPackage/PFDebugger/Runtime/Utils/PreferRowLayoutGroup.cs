using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    public enum DebuggerTabRowDirection
    {
        LeftToRight,
        RightToLeft,
    }

    [AddComponentMenu("Layout/PreferRowLayoutGroup")]
    public class PreferRowLayoutGroup : LayoutGroup
    {
        [SerializeField] private float minCellWidth = 100;
        [SerializeField] private float cellHeight = 100;
        [SerializeField] private Vector2 spacing = Vector2.zero;
        [SerializeField] private bool fillLastRow = true;
        [SerializeField] private DebuggerTabRowDirection rowDirection = DebuggerTabRowDirection.LeftToRight;

        public float MinCellWidth
        {
            get => minCellWidth;
            set
            {
                minCellWidth = Mathf.Max(1f, value);
                SetDirty();
            }
        }

        public float CellHeight
        {
            get => cellHeight;
            set
            {
                cellHeight = Mathf.Max(1f, value);
                SetDirty();
            }
        }

        public Vector2 Spacing
        {
            get => spacing;
            set
            {
                spacing = new Vector2(Mathf.Max(0f, value.x), Mathf.Max(0f, value.y));
                SetDirty();
            }
        }

        public bool FillLastRow
        {
            get => fillLastRow;
            set
            {
                fillLastRow = value;
                SetDirty();
            }
        }

        public DebuggerTabRowDirection RowDirection
        {
            get => rowDirection;
            set
            {
                rowDirection = value;
                SetDirty();
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float minWidth = padding.horizontal + minCellWidth;
            SetLayoutInputForAxis(minWidth, minWidth, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int rowCount = GetRowCount();
            float preferredHeight = padding.vertical;
            if (rowCount > 0)
                preferredHeight += rowCount * cellHeight + (rowCount - 1) * spacing.y;

            SetLayoutInputForAxis(preferredHeight, preferredHeight, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            LayoutChildren(setHorizontal: true);
        }

        public override void SetLayoutVertical()
        {
            LayoutChildren(setHorizontal: false);
        }

        private void LayoutChildren(bool setHorizontal)
        {
            int childCount = rectChildren.Count;
            if (childCount == 0)
                return;

            float availableWidth = rectTransform.rect.width - padding.horizontal;
            if (availableWidth <= 0f)
                availableWidth = minCellWidth;

            int columns = GetColumnCount(availableWidth);
            int rowCount = Mathf.CeilToInt(childCount / (float)columns);
            float totalHeight = rowCount * cellHeight + Mathf.Max(0, rowCount - 1) * spacing.y;
            float startY = GetStartOffset(1, totalHeight);

            for (int row = 0; row < rowCount; row++)
            {
                int startIndex = row * columns;
                int itemCount = Mathf.Min(columns, childCount - startIndex);
                bool isLastRow = row == rowCount - 1;
                float itemWidth = !fillLastRow && isLastRow
                    ? minCellWidth
                    : (availableWidth - Mathf.Max(0, itemCount - 1) * spacing.x) / itemCount;
                float rowWidth = itemCount * itemWidth + Mathf.Max(0, itemCount - 1) * spacing.x;
                float startX = GetStartOffset(0, rowWidth);
                float y = startY + row * (cellHeight + spacing.y);
                float x = rowDirection == DebuggerTabRowDirection.LeftToRight
                    ? startX
                    : startX + rowWidth - itemWidth;

                for (int i = 0; i < itemCount; i++)
                {
                    RectTransform child = rectChildren[startIndex + i];
                    if (setHorizontal)
                    {
                        SetChildAlongAxis(child, 0, x, itemWidth);
                        x += rowDirection == DebuggerTabRowDirection.LeftToRight
                            ? itemWidth + spacing.x
                            : -(itemWidth + spacing.x);
                    }
                    else
                    {
                        SetChildAlongAxis(child, 1, y, cellHeight);
                    }
                }
            }
        }

        private int GetRowCount()
        {
            int childCount = rectChildren.Count;
            if (childCount == 0)
                return 0;

            float availableWidth = rectTransform.rect.width - padding.horizontal;
            if (availableWidth <= 0f)
                availableWidth = minCellWidth;

            int columns = GetColumnCount(availableWidth);
            return Mathf.CeilToInt(childCount / (float)columns);
        }

        private int GetColumnCount(float availableWidth)
        {
            float slotWidth = Mathf.Max(1f, minCellWidth) + spacing.x;
            int columns = Mathf.FloorToInt((availableWidth + spacing.x) / slotWidth);
            return Mathf.Max(1, columns);
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            minCellWidth = Mathf.Max(1f, minCellWidth);
            cellHeight = Mathf.Max(1f, cellHeight);
            spacing.x = Mathf.Max(0f, spacing.x);
            spacing.y = Mathf.Max(0f, spacing.y);
        }
#endif
    }
}
