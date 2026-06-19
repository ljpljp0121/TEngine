using System;

namespace PFGraph
{
    public interface IGraphAsset
    {
        Type GraphType { get; }

        void SaveGraph(BaseGraph graph);

        BaseGraph LoadGraph();
    }
}