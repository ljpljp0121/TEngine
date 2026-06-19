using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace PFGraph
{
    public abstract class GraphAssetOwner<TGraphAsset, TGraph> : MonoBehaviour, IGraphAssetOwner
        where TGraphAsset : UnityObject, IGraphAsset
        where TGraph : BaseGraphProcessor
    {
        [NonSerialized] private TGraph graph = null;
        [SerializeField] private TGraphAsset graphAsset = null;

        public IGraphAsset GraphAsset => graphAsset;

        public BaseGraphProcessor Graph => T_Graph;

        public TGraphAsset T_GraphAsset => graphAsset;

        public virtual TGraph T_Graph
        {
            get
            {
                if (graph == null && graphAsset != null)
                {
                    var graphData = graphAsset.LoadGraph()?.Clone();
                    var validation = GraphValidationUtil.Repair(graphData);
                    graph = ViewModelFactory.ProduceViewModel(graphData) as TGraph;
                    graph?.AppendDiagnostics(validation.Messages);
                }

                return graph;
            }
            set => graph = value;
        }
    }
}