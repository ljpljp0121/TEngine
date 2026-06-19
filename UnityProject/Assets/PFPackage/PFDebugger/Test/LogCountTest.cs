using System.Collections;
using UnityEngine;

namespace PFDebugger
{
    public class LogCountTest : MonoBehaviour
    {
        private void Start()
        {
            int infoTarget = 55;
            int warnTarget = 55;
            int errorTarget = 55;

            for (int i = 0; i < infoTarget; i++) Debug.Log("[LogCountTest] info");
            for (int i = 0; i < warnTarget; i++) Debug.LogWarning("[LogCountTest] warn");
            for (int i = 0; i < errorTarget; i++) Debug.LogError("[LogCountTest] error");
        }
        
        // private void Update()
        // {
        //     int infoTarget = 5;
        //     int warnTarget = 3;
        //     int errorTarget = 2;
        //
        //     for (int i = 0; i < infoTarget; i++) Debug.Log("[LogCountTest] info");
        //     for (int i = 0; i < warnTarget; i++) Debug.LogWarning("[LogCountTest] warn");
        //     for (int i = 0; i < errorTarget; i++) Debug.LogError("[LogCountTest] error");
        // }
    }
}
