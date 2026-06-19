using System;

namespace PFGraph
{
    [Serializable]
    public sealed class PlacematData
    {
        public long id;
        public string title = "Placemat";
        public InternalVector2Int position;
        public InternalVector2Int size = new InternalVector2Int(420, 260);
        public InternalColor color = new InternalColor(0.22f, 0.35f, 0.6f, 0.18f);
    }
}