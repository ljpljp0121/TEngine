using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PFGAS.Runtime.Tests
{
    public sealed class PFGASSampleScenarioRunner : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool showButtons = true;
        [SerializeField] private float tickSeconds = 1f;
        [SerializeField] private List<string> lastResults = new List<string>();

        private readonly PFGASSampleLifecycleCounters unitACounters = new PFGASSampleLifecycleCounters();
        private readonly PFGASSampleLifecycleCounters unitBCounters = new PFGASSampleLifecycleCounters();

        private PFGASSampleUnitFactory duelFactory;
        private CombatUnit unitA;
        private CombatUnit unitB;
        private Vector2 infoScrollPosition;
        private Vector2 controlsScrollPosition;
        private float elapsedDuelTime;
        private int frameTickCount;
        private string startupStatus = "等待 PFGAS Tag 注册...";

        public IReadOnlyList<string> LastResults => lastResults;

        public CombatUnit UnitA => unitA;

        public CombatUnit UnitB => unitB;

        public PFGASSampleLifecycleCounters UnitACounters => unitACounters;

        public PFGASSampleLifecycleCounters UnitBCounters => unitBCounters;

        public bool IsReady => TagHelper.IsRegistered;

        public bool AutoTickEnabled => true;

        public float ElapsedDuelTime => elapsedDuelTime;

        public int AutoTickCount => frameTickCount;

        private IEnumerator Start()
        {
            if (!runOnStart)
            {
                yield break;
            }

            yield return WaitForTagRegistration();
            ResetDuel();
        }

        private void Update()
        {
            if (!TagHelper.IsRegistered ||
                !IsUnitReady(unitA) ||
                !IsUnitReady(unitB))
            {
                return;
            }

            AdvanceAutoTick(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            CleanupDuel();
        }

        private void OnGUI()
        {
            if (!showButtons)
            {
                return;
            }

            var panel = new Rect(
                16f,
                16f,
                Mathf.Max(360f, Screen.width - 32f),
                Mathf.Max(320f, Screen.height - 32f));

            GUILayout.BeginArea(panel, "PFGAS Tag 示例", GUI.skin.window);

            var contentHeight = Mathf.Max(260f, panel.height - 36f);
            var leftWidth = Mathf.Max(430f, panel.width * 0.56f);
            var rightWidth = Mathf.Max(320f, panel.width - leftWidth - 18f);

            GUILayout.BeginHorizontal();

            infoScrollPosition = GUILayout.BeginScrollView(
                infoScrollPosition,
                GUILayout.Width(leftWidth),
                GUILayout.Height(contentHeight));
            DrawStartupStatus();
            DrawUnitOverview();
            DrawTagVisualization();
            DrawRecentLog();
            GUILayout.EndScrollView();

            controlsScrollPosition = GUILayout.BeginScrollView(
                controlsScrollPosition,
                GUILayout.Width(rightWidth),
                GUILayout.Height(contentHeight));
            DrawControls();
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        [ContextMenu("重置 PFGAS 对战")]
        public void ResetDuel()
        {
            EnsureTagsRegistered();

            CleanupDuel();
            duelFactory = new PFGASSampleUnitFactory();
            unitA = duelFactory.CreateUnit("PFGAS 示例单位 A");
            unitB = duelFactory.CreateUnit("PFGAS 示例单位 B");
            unitA.transform.position = new Vector3(-1.5f, 0f, 0f);
            unitB.transform.position = new Vector3(1.5f, 0f, 0f);
            ResetCounters();
            ResetTickState();
            lastResults.Clear();
            AppendLog("已重置对战：单位 A/B 已创建。");
        }

        [ContextMenu("运行 PFGAS 示例")]
        public void RunAll()
        {
            EnsureTagsRegistered();

            lastResults.Clear();
            var burning = PFGASSamples.RunBurningDot();
            var aura = PFGASSamples.RunLeadershipAura();
            var poison = PFGASSamples.RunStackingPoison();
            var shield = PFGASSamples.RunTargetLocalShield();
            var lifecycle = PFGASSamples.RunLifecycleEvent();

            lastResults.Add("灼烧 DoT：HP " + burning.HpAfterApply + " -> " + burning.HpAfterExpiry);
            lastResults.Add("队长光环：目标 MaxHP " + aura.FrontInitialMaxHp + " -> " + aura.FrontAfterSourceFlush);
            lastResults.Add("毒层叠加：层数 " + poison.SourceAStackCount + "，HP " + poison.HpAfterBothSourcesPeriod);
            lastResults.Add("生命护盾：HP " + shield.HpAfterApply + " -> " + shield.HpAfterMaxHpChange);
            lastResults.Add("生命周期事件：事件 " + lifecycle.EventCountAfterCleanup + "，激活效果 " + lifecycle.ActiveEffectCountAfterCleanup);

            Debug.Log("PFGAS 示例运行完成，请查看此组件的 LastResults。", this);
        }

        public void CastBurningAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateBurningDot(), "A 对 B 施加灼烧 DoT");
        }

        public void CastBurningBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateBurningDot(), "B 对 A 施加灼烧 DoT");
        }

        public void CastPoisonAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateStackingPoison(), "A 对 B 叠加毒层");
        }

        public void CastPoisonBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateStackingPoison(), "B 对 A 叠加毒层");
        }

        public void CastLeadershipAuraAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateLeadershipAura(), "A 给 B 添加队长光环");
        }

        public void CastLeadershipAuraBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateLeadershipAura(), "B 给 A 添加队长光环");
        }

        public void CastShieldA()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitA, PFGASSampleEffects.CreateTargetLocalShield(), "A 给自己施加生命护盾");
        }

        public void CastShieldB()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitB, PFGASSampleEffects.CreateTargetLocalShield(), "B 给自己施加生命护盾");
        }

        public void ActivateLifecycleA()
        {
            EnsureDuel();
            ApplyEffect(
                unitA,
                unitA,
                PFGASSampleEffects.CreatePersistentLifecycleEvent(unitACounters),
                "A 开启持续生命周期监听");
        }

        public void ActivateLifecycleB()
        {
            EnsureDuel();
            ApplyEffect(
                unitB,
                unitB,
                PFGASSampleEffects.CreatePersistentLifecycleEvent(unitBCounters),
                "B 开启持续生命周期监听");
        }

        public void PublishHitAToB()
        {
            EnsureDuel();
            unitB.GameplayEventBus.Publish(PFGASSamples.LifecycleEventName, unitA, unitB);
            AppendLog("A 命中 B，B 事件计数 = " + unitBCounters.EventCount);
        }

        public void PublishHitBToA()
        {
            EnsureDuel();
            unitA.GameplayEventBus.Publish(PFGASSamples.LifecycleEventName, unitB, unitA);
            AppendLog("B 命中 A，A 事件计数 = " + unitACounters.EventCount);
        }

        public void TickDuel()
        {
            EnsureDuel();
            var tickStep = GetTickStep();
            TickDuelInternal(tickStep, tickStep);
            AppendLog("手动推进 " + FormatValue(tickStep) + " 秒。");
        }

        public void AdvanceAutoTick(float deltaSeconds)
        {
            AdvanceAutoTick(deltaSeconds, deltaSeconds);
        }

        public void AdvanceAutoTick(float deltaSeconds, float unscaledDeltaSeconds)
        {
            if (deltaSeconds <= 0f ||
                !TagHelper.IsRegistered ||
                !IsUnitReady(unitA) ||
                !IsUnitReady(unitB))
            {
                return;
            }

            TickDuelInternal(deltaSeconds, unscaledDeltaSeconds);
            frameTickCount++;
        }

        public void IncreaseAMaxHp()
        {
            EnsureDuel();
            IncreaseMaxHp(unitA, "A");
        }

        public void IncreaseBMaxHp()
        {
            EnsureDuel();
            IncreaseMaxHp(unitB, "B");
        }

        public void RemoveAllEffects()
        {
            EnsureDuel();
            unitA.Effects.RemoveAll();
            unitB.Effects.RemoveAll();
            AppendLog("已移除 A/B 所有激活效果。");
        }

        public void AddFireTagToA()
        {
            AddLooseTag(unitA, PFGASTestTagIds.State_DeBuff_Fire, "A");
        }

        public void RemoveFireTagFromA()
        {
            RemoveLooseTag(unitA, PFGASTestTagIds.State_DeBuff_Fire, "A");
        }

        public void AddIceTagToB()
        {
            AddLooseTag(unitB, PFGASTestTagIds.State_DeBuff_Ice, "B");
        }

        public void RemoveIceTagFromB()
        {
            RemoveLooseTag(unitB, PFGASTestTagIds.State_DeBuff_Ice, "B");
        }

        public void AddBuffTagToA()
        {
            AddLooseTag(unitA, PFGASTestTagIds.State_Buff, "A");
        }

        public void RemoveBuffTagFromA()
        {
            RemoveLooseTag(unitA, PFGASTestTagIds.State_Buff, "A");
        }

        public void AddPoisonTagToB()
        {
            AddLooseTag(unitB, PFGASTestTagIds.State_DeBuff_Du, "B");
        }

        public void RemovePoisonTagFromB()
        {
            RemoveLooseTag(unitB, PFGASTestTagIds.State_DeBuff_Du, "B");
        }

        public void ClearAllTags()
        {
            EnsureDuel();
            unitA.Tags.Clear();
            unitB.Tags.Clear();
            AppendLog("已清空 A/B 所有 Tag。");
        }

        public void CleanupDuel()
        {
            if (duelFactory != null)
            {
                duelFactory.Dispose();
                duelFactory = null;
            }

            unitA = null;
            unitB = null;
            ResetTickState();
        }

        private IEnumerator WaitForTagRegistration()
        {
            while (!TagHelper.IsRegistered)
            {
                startupStatus = "等待 GameApp 注册 PFGAS Tag...";
                yield return null;
            }

            startupStatus = "PFGAS Tag 已注册。";
        }

        private void DrawStartupStatus()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("启动状态");
            GUILayout.Label("Tag 注册：" + (TagHelper.IsRegistered ? "已注册" : "未注册"));
            GUILayout.Label(startupStatus);
            GUILayout.Label("Runner 会等 TagHelper.IsRegistered 为 true 后再创建示例单位。");
            DrawMetric("自动 Tick", "每帧 Update");
            DrawMetric("手动 Tick 步长", FormatValue(GetTickStep()) + " 秒");
            DrawMetric("已运行时间", FormatValue(elapsedDuelTime) + " 秒");
            DrawMetric("帧 Tick 次数", frameTickCount.ToString());
            GUILayout.EndVertical();
        }

        private void DrawUnitOverview()
        {
            GUILayout.BeginHorizontal();
            DrawUnitCard("单位 A", unitA, unitACounters);
            DrawUnitCard("单位 B", unitB, unitBCounters);
            GUILayout.EndHorizontal();
        }

        private void DrawUnitCard(
            string title,
            CombatUnit unit,
            PFGASSampleLifecycleCounters counters)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(320f));
            try
            {
                GUILayout.Label(title);

                if (!IsUnitReady(unit))
                {
                    GUILayout.Label(unit == null ? "未创建。" : "初始化中...");
                    return;
                }

                DrawMetric("HP", FormatValue(unit.Attributes.GetCurrentValue(PFAttributeId.HP)));
                DrawMetric("MaxHP", FormatValue(unit.Attributes.GetCurrentValue(PFAttributeId.MaxHP)));
                DrawMetric("激活效果", unit.Effects.ActiveEffectCount.ToString());

                if (!TagHelper.IsRegistered)
                {
                    GUILayout.Label("等待 GameApp 注册 PFGAS Tag 后显示 Tag 详情。");
                    return;
                }

                DrawMetric("松散 Tag", FormatTags(unit.Tags.GetLooseTagsSnapshot()));
                DrawMetric("来源 Tag", FormatTags(unit.Tags.GetSourceTagsSnapshot()));
                DrawMetric("全部 Tags", FormatTags(unit.Tags.GetTagsSnapshot()));
                DrawMetric("拥有 State", FormatBool(unit.Tags.HasTag(PFGASTestTagIds.State)));
                DrawMetric("拥有增益", FormatBool(unit.Tags.HasTag(PFGASTestTagIds.State_Buff)));
                DrawMetric("拥有减益", FormatBool(unit.Tags.HasTag(PFGASTestTagIds.State_DeBuff)));
                DrawMetric("精确拥有 Fire", FormatBool(unit.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Fire)));
                DrawMetric("精确拥有 Ice", FormatBool(unit.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Ice)));
                DrawMetric("生命周期事件", counters.EventCount.ToString());
            }
            catch (Exception ex)
            {
                GUILayout.Label("单位面板暂不可用：" + ex.Message);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        private void DrawTagVisualization()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Tag 检查");

            if (!TagHelper.IsRegistered)
            {
                GUILayout.Label("PFGAS Tag 尚未注册，请先通过 GameApp 启动。");
                GUILayout.EndVertical();
                return;
            }

            DrawMetric("灼烧属于减益", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.State_DeBuff_Fire, PFGASTestTagIds.State_DeBuff)));
            DrawMetric("灼烧属于 State", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.State_DeBuff_Fire, PFGASTestTagIds.State)));
            DrawMetric("Life.HP 属于 Life", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.Life_HP, PFGASTestTagIds.Life)));
            DrawMetric("增益属于减益", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.State_Buff, PFGASTestTagIds.State_DeBuff)));
            DrawMetric("已知 State", FormatTag(PFGASTestTagIds.State));
            DrawMetric("已知增益", FormatTag(PFGASTestTagIds.State_Buff));
            DrawMetric("已知灼烧", FormatTag(PFGASTestTagIds.State_DeBuff_Fire));
            DrawMetric("已知冰冻", FormatTag(PFGASTestTagIds.State_DeBuff_Ice));
            GUILayout.EndVertical();
        }

        private void DrawControls()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("操作");

            DrawMetric("自动 Tick", "每帧 Update");
            DrawMetric("手动 Tick 步长", FormatValue(GetTickStep()) + " 秒");
            DrawButtonRow(("重置", ResetDuel), ("运行示例", RunAll));
            DrawButtonRow(("手动 Tick +" + FormatValue(GetTickStep()) + " 秒", TickDuel));
            DrawButtonRow(("清理效果", RemoveAllEffects), ("清空 Tag", ClearAllTags));

            GUILayout.Space(6f);
            GUILayout.Label("效果操作（会改变 HP / 属性 / Tag）");
            DrawButtonRow(("A 对 B 施加灼烧", CastBurningAToB), ("B 对 A 施加灼烧", CastBurningBToA));
            DrawButtonRow(("A 对 B 施加毒", CastPoisonAToB), ("B 对 A 施加毒", CastPoisonBToA));
            DrawButtonRow(("A 给 B 光环", CastLeadershipAuraAToB), ("B 给 A 光环", CastLeadershipAuraBToA));
            DrawButtonRow(("A 给自己护盾", CastShieldA), ("B 给自己护盾", CastShieldB));
            DrawButtonRow(("A 监听", ActivateLifecycleA), ("B 监听", ActivateLifecycleB));
            DrawButtonRow(("A 命中 B", PublishHitAToB), ("B 命中 A", PublishHitBToA));
            DrawButtonRow(("A MaxHP +50", IncreaseAMaxHp), ("B MaxHP +50", IncreaseBMaxHp));

            GUILayout.Space(6f);
            GUILayout.Label("手动 Tag（只改 Tag，不触发伤害）");
            DrawButtonRow(("A +灼烧 Tag", AddFireTagToA), ("A -灼烧 Tag", RemoveFireTagFromA));
            DrawButtonRow(("B +冰冻 Tag", AddIceTagToB), ("B -冰冻 Tag", RemoveIceTagFromB));
            DrawButtonRow(("A +增益 Tag", AddBuffTagToA), ("A -增益 Tag", RemoveBuffTagFromA));
            DrawButtonRow(("B +中毒 Tag", AddPoisonTagToB), ("B -中毒 Tag", RemovePoisonTagFromB));

            GUILayout.EndVertical();
        }

        private void DrawRecentLog()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("最近日志");
            for (var i = lastResults.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(lastResults[i]);
            }

            GUILayout.EndVertical();
        }

        private void ApplyEffect(
            CombatUnit source,
            CombatUnit target,
            GameplayEffect effect,
            string description)
        {
            EnsureDuel();
            var result = target.Effects.ApplyToSelf(effect, source);
            if (result.Failed)
            {
                AppendLog(description + " 失败：" + result.Failure);
                return;
            }

            AppendLog(description + " 成功，句柄=" + result.Value.Handle.Value);
        }

        private void IncreaseMaxHp(CombatUnit unit, string label)
        {
            EnsureDuel();
            unit.Attributes.AddBaseValue(PFAttributeId.MaxHP, 50f);
            TickDuelInternal(0f, 0f);
            AppendLog(label + " MaxHP +50，并刷新动态来源修饰器。");
        }

        private void TickDuelInternal(float deltaSeconds, float unscaledDeltaSeconds)
        {
            unitA.Tick(deltaSeconds, unscaledDeltaSeconds);
            unitB.Tick(deltaSeconds, unscaledDeltaSeconds);
            elapsedDuelTime += deltaSeconds;
        }

        private void AddLooseTag(CombatUnit unit, PFTagId tagId, string label)
        {
            EnsureDuel();
            if (unit.Tags.AddLooseTag(tagId))
            {
                AppendLog(label + " 添加 Loose Tag：" + FormatTag(tagId));
                return;
            }

            AppendLog(label + " 已经拥有 Loose Tag：" + FormatTag(tagId));
        }

        private void RemoveLooseTag(CombatUnit unit, PFTagId tagId, string label)
        {
            EnsureDuel();
            if (unit.Tags.RemoveLooseTag(tagId))
            {
                AppendLog(label + " 移除 Loose Tag：" + FormatTag(tagId));
                return;
            }

            AppendLog(label + " 没有这个 Loose Tag：" + FormatTag(tagId));
        }

        private void EnsureDuel()
        {
            EnsureTagsRegistered();

            if (unitA == null || unitB == null)
            {
                ResetDuel();
            }
        }

        private static void EnsureTagsRegistered()
        {
            if (!TagHelper.IsRegistered)
            {
                throw new InvalidOperationException(
                    "PFGAS Tag 尚未注册。请先让 GameApp 初始化 PFGASTagGenerated.RegisterFromLubanTable()，再使用 PFGASSampleScenarioRunner。");
            }
        }

        private void ResetCounters()
        {
            unitACounters.ApplyCount = 0;
            unitACounters.RemoveCount = 0;
            unitACounters.EventCount = 0;
            unitACounters.DeactivateCount = 0;
            unitBCounters.ApplyCount = 0;
            unitBCounters.RemoveCount = 0;
            unitBCounters.EventCount = 0;
            unitBCounters.DeactivateCount = 0;
        }

        private void ResetTickState()
        {
            elapsedDuelTime = 0f;
            frameTickCount = 0;
        }

        private float GetTickStep()
        {
            return Mathf.Max(0.01f, tickSeconds);
        }

        private static bool IsUnitReady(CombatUnit unit)
        {
            return unit != null &&
                   unit.Attributes != null &&
                   unit.Effects != null &&
                   unit.Tags != null &&
                   unit.GameplayEventBus != null;
        }

        private void AppendLog(string message)
        {
            lastResults.Add(message);
            if (lastResults.Count > 14)
            {
                lastResults.RemoveAt(0);
            }
        }

        private void DrawButtonRow(params (string Text, Action Action)[] buttons)
        {
            GUILayout.BeginHorizontal();
            for (var i = 0; i < buttons.Length; i++)
            {
                DrawButton(buttons[i]);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawButton((string Text, Action Action) button)
        {
            if (!GUILayout.Button(button.Text, GUILayout.Height(30f)))
            {
                return;
            }

            try
            {
                button.Action();
            }
            catch (Exception ex)
            {
                AppendLog("错误：" + ex.Message);
                Debug.LogException(ex, this);
            }
        }

        private static void DrawMetric(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(160f));
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }

        private static string FormatTags(PFTagId[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return "无";
            }

            var names = new string[tags.Length];
            for (var i = 0; i < tags.Length; i++)
            {
                names[i] = FormatTag(tags[i]);
            }

            return string.Join(", ", names);
        }

        private static string FormatTag(PFTagId tagId)
        {
            if (!TagHelper.IsRegistered)
            {
                return tagId.ToString();
            }

            var tagName = TagHelper.GetTagFullName(tagId);
            return string.IsNullOrEmpty(tagName)
                ? tagId.ToString()
                : tagName + " [" + tagId + "]";
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##");
        }

        private static string FormatBool(bool value)
        {
            return value ? "是" : "否";
        }
    }
}
