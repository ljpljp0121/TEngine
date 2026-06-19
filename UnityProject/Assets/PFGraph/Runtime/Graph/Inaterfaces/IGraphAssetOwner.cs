namespace PFGraph
{
    public interface IGraphAssetOwner : IGraphOwner
    {
        IGraphAsset GraphAsset { get; }
    }
}