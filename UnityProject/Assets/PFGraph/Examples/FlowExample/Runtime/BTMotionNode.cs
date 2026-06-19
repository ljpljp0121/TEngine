using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
using PFGraph;

[Serializable]
[NodeMenu("动画状态")]
public class BTMotionNode : BTBaseNode
{
    [LabelText("动画名称")]
    public string configmotionName;

    [Sirenix.OdinInspector.ReadOnly]
    public string resPath;
}

[ViewModel(typeof(BTMotionNode))]
public class BTMotionNodeProcessor : BTBaseNodeProcessor
{
    public BTMotionNodeProcessor(BTMotionNode model) : base(model)
    {
        
    }

    protected override void Execute()
    {
        FlowNext();
    }
}
