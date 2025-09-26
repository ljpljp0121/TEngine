using TEngine;

public class InitOnLoadGameplay
{
    private static bool loaded = false;

    public static void Init()
    {
        if (!loaded)
        {
            InitOnLoadMethod.ProcessInitOnLoadMethod(typeof(InitOnLoadGameplay));
            Log.Info("InitOnLoad : Client_Gameplay");
            loaded = true;
        }
    }
}