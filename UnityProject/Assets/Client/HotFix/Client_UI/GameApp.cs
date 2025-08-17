using System.Collections.Generic;
using System.Reflection;
using Client_Event;
using GameConfig.UI;
using GameLogic;
#if ENABLE_OBFUZ
using Obfuz;
#endif
using TEngine;
using UnityEngine;

#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameApp
{
    private static List<Assembly> _hotfixAssembly;

    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        GameEventHelper.Init();
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Info("======= EnterHotFix =======");
        InitOnLoad();
        Utility.Unity.AddDestroyListener(Release);
        Log.Info("======= StartGame =======");
        StartGameLogic();
    }

    private static void StartGameLogic()
    {
        // GameModule.UI.Active();
        GameEvent.AddEventListener(ILoginUI_Event.ShowLoginUI, () =>
        {
            Debug.Log("1111111111111111");
            UIModule.Instance.ShowUIAsync<LoginUI>();
        });
        GameEvent.Get<ILoginUI>().ShowLoginUI();
        // UIModule.Instance.ShowUIAsync<BattleMainUI>();
        // Debug.Log(GameModule.Table.GetTable<TbUIWnd>().Get("StartPanel").Path);
    }

    private static void InitOnLoad()
    {
        InitOnLoadConfig.Init();
        InitOnLoadData.Init();
        InitOnLoadLogic.Init();
        InitOnLoadGameplay.Init();
        InitOnLoadUI.Init();
    }


    private static void Release()
    {
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }

    public class TestEvent : GameEventArgs
    {
        public override void Clear() { }
    }
}