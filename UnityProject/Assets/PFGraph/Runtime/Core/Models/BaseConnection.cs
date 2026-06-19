using System;

namespace PFGraph
{
    [Serializable]
    public class BaseConnection
    {
        [UnityEngine.HideInInspector] public long fromNode;
        [UnityEngine.HideInInspector] public string fromPort;
        [UnityEngine.HideInInspector] public long toNode;
        [UnityEngine.HideInInspector] public string toPort;
    }
}