using PFGraph;
using UnityEngine;

public class SampleGraphTest : GraphAssetOwner<SampleGraphAsset, SampleGraphProcessor>
{
    private void Update()
    {
        foreach (var node in T_Graph.Nodes.Values)
        {
            if (node is LogNodeProcessor debugNode)
            {
                debugNode.DebugInput();
            }
        }
    }
}
