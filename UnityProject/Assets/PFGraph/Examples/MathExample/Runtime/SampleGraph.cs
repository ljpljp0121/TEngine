using PFGraph;
using System;

[Serializable]
public class SampleGraph : BaseGraph { }

[ViewModel(typeof(SampleGraph))]
public class SampleGraphProcessor : BaseGraphProcessor
{
    public SampleGraphProcessor(BaseGraph model) : base(model) { }
}
