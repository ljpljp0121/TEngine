using System;
using TEngine;

public class GameDataService : Singleton<GameDataService>
{
    private static int CurSaveId = 0;
    
    private GameData gameData;
    private RuntimeData runtimeData;

    protected override void OnInit()
    {
        var saveItem = LoadSaveItem(CurSaveId);
        if (saveItem != null)
        {   
            LoadGameData(saveItem);
        }
        else
        {
            gameData = new GameData();
        }
        runtimeData = new RuntimeData();
    }

    private SaveModule.SaveItem LoadSaveItem(int saveId)
    {
        try
        {
            var saveItem = GameModule.Save.GetSaveItem(saveId);
            if (saveItem == null)
            {
                saveItem = GameModule.Save.CreateSaveItem();
                Log.Warning($"No save item found, created new save item , saveID={saveItem.SaveID}" +
                            $", createTime={saveItem.CreateDateTime}, lastSaveTime={saveItem.LastSaveTime}");
            }
            return saveItem;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load save item: {e.Message}");
        }
        return null;
    }

    private void LoadGameData(SaveModule.SaveItem saveItem)
    {
        try
        {
            gameData = GameModule.Save.LoadObject<GameData>(saveItem);
            
            if (gameData == null)
            {
                CreateNewGameData();
                Log.Warning("Save item exists but no game data found, created new game data");
            }
            else
            {
                Log.Info("Game data loaded successfully");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load game data: {e.Message}");
            CreateNewGameData();
        }
    }

    private void CreateNewGameData()
    {
        gameData = new GameData();
        SaveGameData();
        Log.Info("Created new game data");
    }

    /// <summary>
    /// 存档
    /// </summary>
    public void SaveGameData()
    {
        try
        {
            if (gameData != null)
            {
                GameModule.Save.SaveObject(gameData, CurSaveId);
                Log.Info("Game data saved successfully");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save game data: {e.Message}");
        }
    }

    public override void Release()
    {
        SaveGameData();
        gameData = null;
        runtimeData = null;
    }
}