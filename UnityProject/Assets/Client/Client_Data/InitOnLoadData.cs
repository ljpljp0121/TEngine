using TEngine;

public class InitOnLoadData
{
    private static bool loaded = false;

    public static void Init()
    {
        if (!loaded)
        {
            InitOnLoadMethod.ProcessInitOnLoadMethod(typeof(InitOnLoadData));
            Log.Info("InitOnLoad : Client_Data");
            loaded = true;
        }
    }
}