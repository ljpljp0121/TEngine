using UnityEngine;

namespace PFDebugger
{
    public class GmPanel : MonoBehaviour
    {
        [SerializeField] private GmBar gmBar;

        public GmBar GmBar => gmBar;
    }
}
