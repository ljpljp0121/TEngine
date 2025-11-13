using TEngine;
using UnityEngine;

internal class GameEntry : MonoBehaviour
{
    void Awake()
    {
        Settings.ProcedureSetting.StartProcedure().Forget();
        DontDestroyOnLoad(this);
    }
}