
using System;
using System.Collections.Generic;

/// <summary>
/// 游戏数据类
/// </summary>
[Serializable]
internal class GameData
{
    public PlayerData playerData = new();
    public InventoryData inventoryData = new();
    public MissionData missionData = new();
    public SettingsData settingsData = new();
    public ShopData shopData = new();
}

/// <summary>
/// 玩家数据
/// </summary>
[Serializable]
internal class PlayerData
{
    public int level = 1;
    public int exp = 0;
    public string playerName = "";
    public long gold = 0;
    public long diamond = 0;
}

/// <summary>
/// 背包数据
/// </summary>
[Serializable]
internal class InventoryData
{
    public Dictionary<int, int> items = new();
    public List<EquipmentData> equipments = new();
}

/// <summary>
/// 装备数据
/// </summary>
[Serializable]
internal class EquipmentData
{
    public int id;
    public int configId;
    public int level = 1;
    public int exp = 0;
    public List<int> attributes = new();
}

/// <summary>
/// 任务数据
/// </summary>
[Serializable]
internal class MissionData
{
    public List<int> completedMissions = new();
    public List<int> acceptedMissions = new();
    public Dictionary<int, int> missionProgress = new();
}

/// <summary>
/// 设置数据
/// </summary>
[Serializable]
internal class SettingsData
{
    public float musicVolume = 1.0f;
    public float soundVolume = 1.0f;
    public bool vibration = true;
    public string language = "zh-cn";
}

/// <summary>
/// 商店数据
/// </summary>
[Serializable]
internal class ShopData
{
    public Dictionary<int, int> purchaseCount = new();
    public Dictionary<int, long> refreshTime = new();
}
