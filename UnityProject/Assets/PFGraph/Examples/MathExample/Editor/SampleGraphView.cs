#if UNITY_EDITOR
using PFGraph;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class SampleGraphView : BaseGraphView
{
    protected override void BuildNodeMenu(NodeMenuWindow nodeMenu)
    {
        foreach (var nodeDataType in GetNodeDataTypes())
        {
            if (nodeDataType.IsAbstract)
                continue;
            var nodeStaticInfo = GraphProcessorUtil.GetNodeStaticInfo(nodeDataType);
            if (nodeStaticInfo.Hidden)
                continue;

            var path = nodeStaticInfo.Path;
            var menu = nodeStaticInfo.Menu;
            nodeMenu.entries.Add(new NodeMenuWindow.NodeEntry(path, menu, nodeDataType));
        }
    }

    private IEnumerable<Type> GetNodeDataTypes()
    {
        yield return typeof(FloatNode);
        yield return typeof(AddNode);
        yield return typeof(SubNode);
        yield return typeof(LogNode);
    }

    protected override BaseConnectionView NewConnectionView(BaseConnectionProcessor connection)
    {
        return new SampleConnectionView();
    }
}
#endif