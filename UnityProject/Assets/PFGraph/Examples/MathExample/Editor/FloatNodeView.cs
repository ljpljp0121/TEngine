
#if UNITY_EDITOR
using System.Collections.ObjectModel;
using PFGraph;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomView(typeof(FloatNode))]
public class FloatNodeView : BaseNodeView
{
    public FloatField valueField;

    public FloatNodeView()
    {
        valueField = new FloatField();
        valueField.style.marginLeft = 3;
        valueField.style.marginRight = 3;
        valueField.RegisterValueChangedCallback(OnFloatFieldChanged);
        controls.Add(valueField);
    }

    protected override void DoInit()
    {
        base.DoInit();
        var v = ViewModel as FloatNodeProcessor;
        this.valueField.SetValueWithoutNotify(v.Value);
        this.ViewModel.RegisterValueChanged<float>(nameof(FloatNode.num), OnFloatNumChanged);
    }

    protected override void DoUnInit()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        a.Add(0);
        this.ViewModel.UnregisterValueChanged<float>(nameof(FloatNode.num), OnFloatNumChanged);
        base.DoUnInit();
    }

    private void OnFloatFieldChanged(ChangeEvent<float> evt)
    {
        var v = ViewModel as FloatNodeProcessor;
        v.Value = evt.newValue;
    }

    private void OnFloatNumChanged(ViewModel.ValueChangedArg<float> arg)
    {
        valueField.SetValueWithoutNotify(arg.newValue);
    }
}
#endif