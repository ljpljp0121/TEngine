using System;

namespace PFGraph
{
    [Serializable]
    public abstract class BaseNode
    {
        [UnityEngine.HideInInspector] public long id;
        [UnityEngine.HideInInspector] public InternalVector2Int position;
    }
}