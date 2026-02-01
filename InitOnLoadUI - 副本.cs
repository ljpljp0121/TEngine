using TEngine;

public class InitOnLoadUI
{
    private static bool loaded = false;

    public static void Init()
    {
        if (!loaded)
        {
            InitOnLoadMethod.ProcessInitOnLoadMethod(typeof(InitOnLoadUI));
            Log.Info("InitOnLoad : Client_UI");
            loaded = true;
        }
    }
}