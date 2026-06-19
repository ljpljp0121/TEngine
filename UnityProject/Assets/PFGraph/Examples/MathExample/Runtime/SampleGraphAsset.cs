using PFGraph;
using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu]
public class SampleGraphAsset : ScriptableObject, IGraphAsset
{
    [SerializeField]
    private SampleGraph data;
    
    public Type GraphType => typeof(SampleGraph);

    public void SaveGraph(BaseGraph graph) => this.data = (SampleGraph)graph;

    public BaseGraph LoadGraph() => data;

    [Button]
    public void Reset()
    {
        SaveGraph(new SampleGraph());
    }
}