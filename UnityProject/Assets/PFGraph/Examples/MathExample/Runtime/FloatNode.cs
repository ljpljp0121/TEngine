using PFGraph;
using UnityEngine;

[NodeTooltip("浮点数节点")]
[NodeMenu("Float")]
public class FloatNode : BaseNode
{
    public float num;
}

[ViewModel(typeof(FloatNode))]
public class FloatNodeProcessor : BaseNodeProcessor, IGetPortValue, IGetPortValue<float>
{
    public FloatNode TModel { get; }

    public float Value
    {
        get => TModel.num;
        set => SetFieldValue(ref TModel.num, value, nameof(FloatNode.num));
    }

    public FloatNodeProcessor(FloatNode model) : base(model)
    {
        TModel = model;
        AddPort(new PortProcessor(ConstValues.FLOW_OUT_PORT_NAME, BasePort.Direction.Right, BasePort.Capacity.Multi, typeof(object)));
    }

    public object GetValue(string port)
    {
        return TModel.num;
    }

    float IGetPortValue<float>.GetValue(string port)
    {
        return TModel.num;
    }
}