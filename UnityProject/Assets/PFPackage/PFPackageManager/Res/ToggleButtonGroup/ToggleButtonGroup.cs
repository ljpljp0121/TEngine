#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace PFPackage
{
    /// <summary>
    /// ToggleButtonGroup组件 - 管理一组按钮，确保同时只有一个按钮处于选中状态
    /// </summary>
    public class ToggleButtonGroup : VisualElement
    {
        #region UXML支持

        public new class UxmlFactory : UxmlFactory<ToggleButtonGroup, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription m_SelectedIndex = new UxmlIntAttributeDescription
            {
                name = "selectedIndex", defaultValue = -1
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var toggleButtonGroup = ve as ToggleButtonGroup;
                if (toggleButtonGroup != null)
                {
                    toggleButtonGroup.selectedIndex = m_SelectedIndex.GetValueFromBag(bag, cc);
                }
            }
        }

        #endregion

        #region 事件定义

        /// <summary>
        /// 当选中索引改变时触发的事件
        /// </summary>
        public event EventCallback<ChangeEvent<int>> onSelectedIndexChanged
        {
            add => RegisterCallback(value);
            remove => UnregisterCallback(value);
        }

        #endregion

        #region 私有字段

        private List<VisualElement> m_ToggleableItems = new List<VisualElement>();
        private int m_SelectedIndex = -1;

        // USS类名常量
        private const string k_SelectedItemUssClassName = "toggle-button-group__item--selected";

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前选中项的索引，-1表示没有选中项
        /// </summary>
        public int selectedIndex
        {
            get => m_SelectedIndex;
            set
            {
                if (m_SelectedIndex == value)
                    return;

                var previousIndex = m_SelectedIndex;
                m_SelectedIndex = value;

                UpdateSelectionState();

                // 触发事件
                using (ChangeEvent<int> evt = ChangeEvent<int>.GetPooled(previousIndex, m_SelectedIndex))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
        }

        /// <summary>
        /// 当前选中的元素，只读
        /// </summary>
        public VisualElement selectedItem
        {
            get
            {
                if (m_SelectedIndex >= 0 && m_SelectedIndex < m_ToggleableItems.Count)
                    return m_ToggleableItems[m_SelectedIndex];
                return null;
            }
        }

        /// <summary>
        /// 所有可切换的子元素数量
        /// </summary>
        public int itemCount => m_ToggleableItems.Count;

        #endregion

        #region 构造函数

        public ToggleButtonGroup()
        {
            // 注册子元素变化事件
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        #endregion

        #region 事件处理

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RefreshToggleableItems();
            RegisterItemCallbacks();
            UpdateSelectionState();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterItemCallbacks();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size != evt.newRect.size)
            {
                RefreshToggleableItems();
                RegisterItemCallbacks();
                UpdateSelectionState();
            }
        }

        #endregion

        #region 核心逻辑

        /// <summary>
        /// 刷新可切换的子元素列表
        /// </summary>
        private void RefreshToggleableItems()
        {
            UnregisterItemCallbacks();
            m_ToggleableItems.Clear();

            // 查找所有Button和Toggle类型的子元素
            foreach (var child in Children())
            {
                if (child is Button || child is Toggle)
                {
                    m_ToggleableItems.Add(child);
                }
            }
        }

        /// <summary>
        /// 注册子元素的点击事件
        /// </summary>
        private void RegisterItemCallbacks()
        {
            for (int i = 0; i < m_ToggleableItems.Count; i++)
            {
                var item = m_ToggleableItems[i];
                if (item != null)
                {
                    item.RegisterCallback<ClickEvent>(OnItemClick);
                }
            }
        }

        /// <summary>
        /// 取消注册子元素的点击事件
        /// </summary>
        private void UnregisterItemCallbacks()
        {
            foreach (var item in m_ToggleableItems)
            {
                if (item != null)
                {
                    item.UnregisterCallback<ClickEvent>(OnItemClick);
                }
            }
        }

        /// <summary>
        /// 处理子元素点击事件
        /// </summary>
        private void OnItemClick(ClickEvent evt)
        {
            var clickedItem = evt.target as VisualElement;
            if (clickedItem == null)
                return;

            // 如果点击的是子元素的子元素，需要向上查找直到找到可切换的元素
            VisualElement targetItem = clickedItem;
            while (targetItem != null && !m_ToggleableItems.Contains(targetItem))
            {
                targetItem = targetItem.parent;
            }

            int index = m_ToggleableItems.IndexOf(targetItem);
            if (index >= 0)
            {
                // 如果点击的是已选中的项，则取消选择
                if (m_SelectedIndex == index)
                {
                    selectedIndex = -1;
                }
                else
                {
                    selectedIndex = index;
                }
            }
        }

        /// <summary>
        /// 更新选中状态的UI显示
        /// </summary>
        private void UpdateSelectionState()
        {
            // 移除所有项的选中样式
            foreach (var item in m_ToggleableItems)
            {
                if (item != null)
                {
                    item.RemoveFromClassList(k_SelectedItemUssClassName);
                }
            }
            
            // 为当前选中项添加选中样式
            if (m_SelectedIndex >= 0 && m_SelectedIndex < m_ToggleableItems.Count)
            {
                var selectedItem = m_ToggleableItems[m_SelectedIndex];
                if (selectedItem != null)
                {
                    selectedItem.AddToClassList(k_SelectedItemUssClassName);
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置指定索引的项为选中状态
        /// </summary>
        /// <param name="index">要选中的项索引</param>
        public void SetSelectedIndex(int index)
        {
            selectedIndex = index;
        }

        /// <summary>
        /// 清除所有选中状态
        /// </summary>
        public void ClearSelection()
        {
            selectedIndex = -1;
        }

        /// <summary>
        /// 获取指定索引的子元素
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>对应的子元素，如果索引无效则返回null</returns>
        public VisualElement GetItemAt(int index)
        {
            if (index >= 0 && index < m_ToggleableItems.Count)
                return m_ToggleableItems[index];
            return null;
        }

        #endregion
    }
}
#endif