using TEngine;

public class InitOnLoadLogic
{
    private static bool loaded = false;

    public static void Init()
    {
        if (!loaded)
        {
            InitOnLoadMethod.ProcessInitOnLoadMethod(typeof(InitOnLoadLogic));
            Log.Info("InitOnLoad : Client_Logic");
            loaded = true;
        }
    }
}