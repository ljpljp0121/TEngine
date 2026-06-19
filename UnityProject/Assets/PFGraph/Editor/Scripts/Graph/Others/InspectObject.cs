using UnityEngine;

namespace PFGraph
{
    public class InspectObject : ScriptableObject
    {
        [SerializeReference] public BaseGraph graph;
    }
}