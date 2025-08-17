using Client_Gameplay;
using GameLogic;
using TEngine;
using UnityEngine;

/// <summary>
/// 实体管理器示例
/// </summary>
public class SimpleEntityManager : MonoBehaviour
{
    private int _nextEntityId = 1;

    void Start()
    {
        // 添加实体组
        if (!EntityModule.Instance.HasEntityGroup("Player"))
        {
            EntityModule.Instance.AddEntityGroup("Player", 60f, 10, 60f, 0);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreatePlayer();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            HideAllPlayers();
        }
    }

    private void CreatePlayer()
    {
        int entityId = _nextEntityId++;

        // 显示实体
        EntityModule.Instance.ShowEntity<SimplePlayerLogic>(
            entityId,
            "Player", // 实体资源名称 
            "Player", // 实体组名称
            null // 用户数据
        );

        Log.Info($"创建玩家实体: {entityId}");
    }

    private void HideAllPlayers()
    {
        var players = EntityModule.Instance.GetEntities("Player");
        foreach (var player in players)
        {
            EntityModule.Instance.HideEntity(player);
        }

        Log.Info("隐藏所有玩家实体");
    }
}