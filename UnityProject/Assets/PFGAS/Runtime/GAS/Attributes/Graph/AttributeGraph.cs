using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>属性图的共享状态入口；具体流程按 partial 文件拆分，运行时仍是同一个 AttributeGraph。</summary>
    public sealed partial class AttributeGraph
    {
        private readonly Dictionary<PFAttributeId, AttributeNode> nodes = new();
        private readonly AttributeGraphContext context;

        private readonly HashSet<PFAttributeId> reusableDirtySet = new();
        private readonly List<AttributeNode> reusableSelectedNodes = new();
        private readonly Stack<PFAttributeId> reusableDirtyStack = new();

        private readonly Dictionary<ModifierSourceHandle, ModifierSource> modifierSources = new();
        private readonly Dictionary<PFAttributeId, List<AttributeModifier>> modifiersByAttribute = new();

        private readonly Dictionary<PFAttributeId, AttributeValue> originalChangedValues = new();
        private readonly List<AttributeChange> reusableChanges = new();

        private List<AttributeNode> cachedTopologicalOrder = new();
        private bool topologyDirty = true;
        private int nextModifierSourceHandle = 1;

        private int batchDepth;
        private bool pendingFullRecalculate;
        private bool pendingPartialRecalculate;
        private bool isPublishingChanges;

        public AttributeGraph()
        {
            context = new AttributeGraphContext(this);
        }
        
        /// <summary>开始批量修改；批处理中只记录 dirty 状态，最外层结束时统一重算和发事件。</summary>
        private void BeginBatchUpdate()
        {
            batchDepth++;
        }

        /// <summary>结束一次批量修改；最外层结束时统一执行待定重算和事件发布。</summary>
        private void EndBatchUpdate()
        {
            if (batchDepth <= 0)
            {
                GASGuard.ThrowInvalidOperation("AttributeGraph batch update was not started.");
            }

            batchDepth--;
            if (batchDepth > 0)
            {
                return;
            }

            if (pendingFullRecalculate)
            {
                pendingFullRecalculate = false;
                pendingPartialRecalculate = false;
                RecalculateAllInternal();
            }
            else if (pendingPartialRecalculate)
            {
                pendingPartialRecalculate = false;
                RecalculateDirtySet();
            }

            PublishAttributeChanges();
        }
    }
}
