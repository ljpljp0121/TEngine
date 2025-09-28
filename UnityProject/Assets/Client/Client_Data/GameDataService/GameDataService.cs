using System;
using TEngine;

public partial class GameDataService : Singleton<GameDataService>
{
    private static int CurSaveId = 0;

    private GameData gameData;
    private SettingData settingData;

    protected override void OnInit()
    {
        settingData = GameModule.Save.LoadSetting<SettingData>();
        if (settingData == null)
        {
            settingData = new SettingData();
            Log.Info("[SaveData] Setting data not found, created new setting data");
        }
        else
        {
            Log.Info($"[SaveData] Setting data loaded successfully");
        }

        //暂时没有多存档，如果有多存档先不要load
        LoadSaveItem(0);
    }

    public void LoadSaveItem(int saveId)
    {
        SaveModule.SaveItem saveItem = GameModule.Save.GetSaveItem(saveId);
        if (saveItem == null)
        {
            saveItem = GameModule.Save.CreateSaveItem(saveId);
            Log.Warning($"[SaveData] No save item found, create new save item, saveID : {saveItem.SaveID}," +
                        $"createTime : {saveItem.CreateDateTime}, lastSaveTime : {saveItem.LastSaveTime}");
        }
        if (saveItem != null)
        {
            LoadGameData(saveItem);
        }
        else
        {
            gameData = new GameData();
            settingData = new SettingData();
            Log.Fatal("[SaveData] Save item load and create failded, please check, this will make game data not save");
        }
    }

    private void LoadGameData(SaveModule.SaveItem saveItem)
    {
        gameData = GameModule.Save.LoadObject<GameData>(saveItem);
        if (gameData == null)
        {
            gameData = new GameData();
            Log.Info("[SaveData] Save item exists but no game data found, created new game data");
        }
        else
            Log.Info($"[SaveData] Game data loaded successfully");

        SaveGameData();
    }

    public void SaveGameData()
    {
        if (gameData != null)
        {
            GameModule.Save.SaveObject(gameData,CurSaveId);
        }
        if (settingData != null)
        {
            GameModule.Save.SaveSetting(settingData);
        }
        ResetAutoSaveTimer();
    }
}