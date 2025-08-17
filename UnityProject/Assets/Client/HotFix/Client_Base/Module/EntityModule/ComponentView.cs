#if UNITY_EDITOR
using UnityEngine;

namespace Client_Base
{
    public class ComponentView: MonoBehaviour
    {
        public Entity Component
        {
            get;
            set;
        }
    }
}
#endif