namespace PFGAS.Runtime
{
    /// <summary>Modifier 对属性执行的聚合操作。</summary>
    public enum GEOperation
    {
        Add = 0,
        Multiply = 1,
        Override = 2,
    }

    /// <summary>属性收集多个 Modifier 时使用的聚合模式。</summary>
    public enum AggregationMode
    {
        Stacking,
        MinValueOnly,
        MaxValueOnly,
    }
}
