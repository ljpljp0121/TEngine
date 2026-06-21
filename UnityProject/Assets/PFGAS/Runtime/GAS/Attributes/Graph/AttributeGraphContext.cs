namespace PFGAS.Runtime
{
    /// <summary>processor 和 Magnitude 读取 AttributeGraph 值的只读上下文。</summary>
    public sealed class AttributeGraphContext
    {
        private readonly AttributeGraph graph;

        internal AttributeGraphContext(AttributeGraph graph)
        {
            this.graph = graph;
        }

        public float GetBaseValue(PFAttributeId attributeId)
        {
            return graph.GetBaseValue(attributeId);
        }

        public float GetCurrentValue(PFAttributeId attributeId)
        {
            return graph.GetCurrentValue(attributeId);
        }

        public bool TryGetValue(PFAttributeId attributeId, out AttributeValue value)
        {
            return graph.TryGetValue(attributeId, out value);
        }
    }
}
