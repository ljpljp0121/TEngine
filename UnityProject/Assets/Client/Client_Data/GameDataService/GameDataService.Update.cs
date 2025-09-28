using System;

public partial class GameDataService : IUpdate
{
    // 下次自动存档时间
    private DateTime nextAutoSaveTime;
    // 自动存档间隔（分钟）
    private const int AUTO_SAVE_INTERVAL_MINUTES = 2;

    public void OnUpdate()
    {
        var now = DateTime.Now;
        if (now >= nextAutoSaveTime)
        {
            SaveGameData();
            TEngine.Log.Info("执行定时存档");
        }
    }


    private void ResetAutoSaveTimer()
    {
        nextAutoSaveTime = DateTime.Now.AddMinutes(AUTO_SAVE_INTERVAL_MINUTES);
    }
}