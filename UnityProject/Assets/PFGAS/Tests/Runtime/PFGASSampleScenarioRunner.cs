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
        [SerializeField] private bool showRiskyDynamicExamples;
        [SerializeField] private float tickSeconds = 1f;
        [SerializeField] private List<string> lastResults = new List<string>();

        private readonly PFGASSampleLifecycleCounters unitACounters = new PFGASSampleLifecycleCounters();
        private readonly PFGASSampleLifecycleCounters unitBCounters = new PFGASSampleLifecycleCounters();
        private readonly List<ActiveGameplayEffect> activeEffectDetails =
            new List<ActiveGameplayEffect>();

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

            GUILayout.BeginArea(panel, "PFGAS 示例场景", GUI.skin.window);

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
            var summaries = PFGASSamples.RunAllSampleSummaries();
            for (var i = 0; i < summaries.Count; i++)
            {
                AppendLog(summaries[i]);
            }

            Debug.Log("PFGAS 示例运行完成，请查看此组件的 LastResults。", this);
        }

        public void StrikeAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateInstantDamage(25f), "A 攻击 B");
        }

        public void StrikeBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateInstantDamage(25f), "B 攻击 A");
        }

        public void HealA()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitA, PFGASSampleEffects.CreateInstantHeal(20f), "A 治疗自己");
        }

        public void HealB()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitB, PFGASSampleEffects.CreateInstantHeal(20f), "B 治疗自己");
        }

        public void CastBurningAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateBurningDot(), "A 对 B 施加快照灼烧");
        }

        public void CastBurningBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateBurningDot(), "B 对 A 施加快照灼烧");
        }

        public void CastScalingBurningAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateScalingBurningDot(), "A 对 B 施加动态周期灼烧");
        }

        public void CastScalingBurningBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateScalingBurningDot(), "B 对 A 施加动态周期灼烧");
        }

        public void CastPoisonAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateStackingPoison(), "A 对 B 叠加中毒");
        }

        public void CastPoisonBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateStackingPoison(), "B 对 A 叠加中毒");
        }

        public void CastDuCengAToB()
        {
            CastPoisonAToB();
        }

        public void CastDuCengBToA()
        {
            CastPoisonBToA();
        }

        public void CastBleedAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateIndependentBleed(), "A 对 B 施加独立流血");
        }

        public void CastBleedBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateIndependentBleed(), "B 对 A 施加独立流血");
        }

        public void CastSnapshotAuraAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateSnapshotLeadershipAura(), "A 给 B 添加快照光环");
        }

        public void CastSnapshotAuraBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateSnapshotLeadershipAura(), "B 给 A 添加快照光环");
        }

        public void CastLeadershipAuraAToB()
        {
            CastSnapshotAuraAToB();
        }

        public void CastLeadershipAuraBToA()
        {
            CastSnapshotAuraBToA();
        }

        public void CastRiskyDynamicAuraAToB()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateRiskyDynamicLeadershipAura(), "A 给 B 添加危险动态光环");
        }

        public void CastRiskyDynamicAuraBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateRiskyDynamicLeadershipAura(), "B 给 A 添加危险动态光环");
        }

        public void CastShieldA()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitA, PFGASSampleEffects.CreateTargetLocalShield(), "A 获得目标本地护盾");
        }

        public void CastShieldB()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitB, PFGASSampleEffects.CreateTargetLocalShield(), "B 获得目标本地护盾");
        }

        public void CastRegenA()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitA, PFGASSampleEffects.CreateRefreshRegen(), "A 获得刷新型再生");
        }

        public void CastRegenB()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitB, PFGASSampleEffects.CreateRefreshRegen(), "B 获得刷新型再生");
        }

        public void CastWeakFortifyA()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitA, PFGASSampleEffects.CreateReplaceFortify(15f), "A 获得弱强化");
        }

        public void CastWeakFortifyB()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitB, PFGASSampleEffects.CreateReplaceFortify(15f), "B 获得弱强化");
        }

        public void CastStrongFortifyA()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitA, PFGASSampleEffects.CreateReplaceFortify(40f), "A 获得强强化");
        }

        public void CastStrongFortifyB()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitB, PFGASSampleEffects.CreateReplaceFortify(40f), "B 获得强强化");
        }

        public void ActivateLifecycleA()
        {
            EnsureDuel();
            ApplyEffect(
                unitA,
                unitA,
                PFGASSampleEffects.CreatePersistentLifecycleEvent(unitACounters),
                "A 开启生命周期监听");
        }

        public void ActivateLifecycleB()
        {
            EnsureDuel();
            ApplyEffect(
                unitB,
                unitB,
                PFGASSampleEffects.CreatePersistentLifecycleEvent(unitBCounters),
                "B 开启生命周期监听");
        }

        public void PublishHitAToB()
        {
            EnsureDuel();
            unitB.GameplayEventBus.Publish(PFGASSamples.LifecycleEventName, unitA, unitB);
            AppendLog("A 发布命中事件给 B，B 事件计数=" + unitBCounters.EventCount);
        }

        public void PublishHitBToA()
        {
            EnsureDuel();
            unitA.GameplayEventBus.Publish(PFGASSamples.LifecycleEventName, unitB, unitA);
            AppendLog("B 发布命中事件给 A，A 事件计数=" + unitACounters.EventCount);
        }

        public void TickDuel()
        {
            EnsureDuel();
            var tickStep = GetTickStep();
            TickDuelInternal(tickStep, tickStep);
            AppendLog("手动 Tick +" + FormatValue(tickStep) + " 秒。");
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

        public void LowerAHP()
        {
            EnsureDuel();
            AddHpBase(unitA, "A", -20f);
        }

        public void LowerBHP()
        {
            EnsureDuel();
            AddHpBase(unitB, "B", -20f);
        }

        public void RestoreBaseAttributes()
        {
            EnsureDuel();
            ResetUnitBaseAttributes(unitA);
            ResetUnitBaseAttributes(unitB);
            TickDuelInternal(0f, 0f);
            AppendLog("已将 A/B 的 Base HP 和 MaxHP 恢复为 100。");
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

        public void AddDuTagToB()
        {
            AddLooseTag(unitB, PFGASTestTagIds.State_DeBuff_Du, "B");
        }

        public void RemoveDuTagFromB()
        {
            RemoveLooseTag(unitB, PFGASTestTagIds.State_DeBuff_Du, "B");
        }

        public void ClearAllTags()
        {
            EnsureDuel();
            unitA.Tags.Clear();
            unitB.Tags.Clear();
            AppendLog("已清空 A/B 所有松散 Tag 和来源 Tag。");
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
            DrawMetric("Tag 注册", TagHelper.IsRegistered ? "已注册" : "未注册");
            DrawMetric("状态", startupStatus);
            DrawMetric("自动 Tick", "每帧 Update");
            DrawMetric("手动 Tick 步长", FormatValue(GetTickStep()) + " 秒");
            DrawMetric("已运行时间", FormatValue(elapsedDuelTime) + " 秒");
            DrawMetric("帧 Tick 次数", frameTickCount.ToString());
            GUILayout.Label("设计约定：默认示例避开跨单位 SourceAttribute + DynamicWhileActive 互相光环循环。");
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

                DrawMetric("HP Base/Current", FormatValue(unit.Attributes.GetBaseValue(PFAttributeId.HP)) + " / " + FormatValue(unit.Attributes.GetCurrentValue(PFAttributeId.HP)));
                DrawMetric("MaxHP Base/Current", FormatValue(unit.Attributes.GetBaseValue(PFAttributeId.MaxHP)) + " / " + FormatValue(unit.Attributes.GetCurrentValue(PFAttributeId.MaxHP)));
                DrawMetric("激活效果", unit.Effects.ActiveEffectCount.ToString());
                DrawMetric("效果详情", FormatActiveEffects(unit));

                if (!TagHelper.IsRegistered)
                {
                    GUILayout.Label("等待 Tag 注册后显示 Tag 详情。");
                    return;
                }

                DrawMetric("松散 Tag", FormatTags(unit.Tags.GetLooseTagsSnapshot()));
                DrawMetric("来源 Tag", FormatTags(unit.Tags.GetSourceTagsSnapshot()));
                DrawMetric("全部 Tag", FormatTags(unit.Tags.GetTagsSnapshot()));
                DrawMetric("拥有 State", FormatBool(unit.Tags.HasTag(PFGASTestTagIds.State)));
                DrawMetric("拥有 Buff", FormatBool(unit.Tags.HasTag(PFGASTestTagIds.State_Buff)));
                DrawMetric("拥有 Debuff", FormatBool(unit.Tags.HasTag(PFGASTestTagIds.State_DeBuff)));
                DrawMetric("精确 Fire", FormatBool(unit.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Fire)));
                DrawMetric("精确 Ice", FormatBool(unit.Tags.HasExactTag(PFGASTestTagIds.State_DeBuff_Ice)));
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
                GUILayout.Label("PFGAS Tag 尚未注册。");
                GUILayout.EndVertical();
                return;
            }

            DrawMetric("Fire 属于 Debuff", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.State_DeBuff_Fire, PFGASTestTagIds.State_DeBuff)));
            DrawMetric("Fire 属于 State", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.State_DeBuff_Fire, PFGASTestTagIds.State)));
            DrawMetric("Life.HP 属于 Life", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.Life_HP, PFGASTestTagIds.Life)));
            DrawMetric("Buff 属于 Debuff", FormatBool(TagHelper.IsOrUnder(PFGASTestTagIds.State_Buff, PFGASTestTagIds.State_DeBuff)));
            DrawMetric("已知 State", FormatTag(PFGASTestTagIds.State));
            DrawMetric("已知 Buff", FormatTag(PFGASTestTagIds.State_Buff));
            DrawMetric("已知 Fire", FormatTag(PFGASTestTagIds.State_DeBuff_Fire));
            DrawMetric("已知 Ice", FormatTag(PFGASTestTagIds.State_DeBuff_Ice));
            GUILayout.EndVertical();
        }

        private void DrawControls()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("操作");
            DrawButtonRow(("重置", ResetDuel), ("运行全部示例", RunAll));
            DrawButtonRow(("手动 Tick +" + FormatValue(GetTickStep()) + " 秒", TickDuel));
            DrawButtonRow(("清理效果", RemoveAllEffects), ("清空 Tag", ClearAllTags));
            DrawButtonRow(("恢复基础属性", RestoreBaseAttributes));

            GUILayout.Space(6f);
            GUILayout.Label("瞬时效果");
            DrawButtonRow(("A 打 B", StrikeAToB), ("B 打 A", StrikeBToA));
            DrawButtonRow(("治疗 A", HealA), ("治疗 B", HealB));
            DrawButtonRow(("A HP -20", LowerAHP), ("B HP -20", LowerBHP));

            GUILayout.Space(6f);
            GUILayout.Label("周期效果");
            DrawButtonRow(("A 快照灼烧 B", CastBurningAToB), ("B 快照灼烧 A", CastBurningBToA));
            DrawButtonRow(("A 动态灼烧 B", CastScalingBurningAToB), ("B 动态灼烧 A", CastScalingBurningBToA));
            DrawButtonRow(("A 中毒 B", CastPoisonAToB), ("B 中毒 A", CastPoisonBToA));
            DrawButtonRow(("A 流血 B", CastBleedAToB), ("B 流血 A", CastBleedBToA));
            DrawButtonRow(("再生 A", CastRegenA), ("再生 B", CastRegenB));

            GUILayout.Space(6f);
            GUILayout.Label("持续效果");
            DrawButtonRow(("A 快照光环 B", CastSnapshotAuraAToB), ("B 快照光环 A", CastSnapshotAuraBToA));
            DrawButtonRow(("护盾 A", CastShieldA), ("护盾 B", CastShieldB));
            DrawButtonRow(("弱强化 A", CastWeakFortifyA), ("强强化 A", CastStrongFortifyA));
            DrawButtonRow(("弱强化 B", CastWeakFortifyB), ("强强化 B", CastStrongFortifyB));
            DrawButtonRow(("A MaxHP +50", IncreaseAMaxHp), ("B MaxHP +50", IncreaseBMaxHp));

            if (showRiskyDynamicExamples)
            {
                GUILayout.Space(6f);
                GUILayout.Label("危险动态来源光环");
                GUILayout.Label("只建议单向使用；双向只用于刻意观察反馈循环。");
                DrawButtonRow(("A 动态光环 B", CastRiskyDynamicAuraAToB), ("B 动态光环 A", CastRiskyDynamicAuraBToA));
            }

            GUILayout.Space(6f);
            GUILayout.Label("生命周期事件");
            DrawButtonRow(("A 监听", ActivateLifecycleA), ("B 监听", ActivateLifecycleB));
            DrawButtonRow(("A 命中事件 B", PublishHitAToB), ("B 命中事件 A", PublishHitBToA));

            GUILayout.Space(6f);
            GUILayout.Label("手动 Tag");
            DrawButtonRow(("A +灼烧", AddFireTagToA), ("A -灼烧", RemoveFireTagFromA));
            DrawButtonRow(("B +冰冻", AddIceTagToB), ("B -冰冻", RemoveIceTagFromB));
            DrawButtonRow(("A +增益", AddBuffTagToA), ("A -增益", RemoveBuffTagFromA));
            DrawButtonRow(("B +中毒", AddDuTagToB), ("B -中毒", RemoveDuTagFromB));

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

            if (target.Effects.TryGetActiveEffect(result.Value.Handle, out var activeEffect))
            {
                AppendLog(
                    description +
                    " -> 激活 " +
                    activeEffect.Effect.EffectId +
                    " 句柄=" + result.Value.Handle.Value +
                    " 层数=" + activeEffect.StackCount +
                    " 剩余=" + FormatRemainingTime(activeEffect));
                return;
            }

            AppendLog(
                description +
                " -> 瞬时变更=" + result.Value.AttributeChanges.Count +
                "，目标 HP=" + FormatValue(target.Attributes.GetBaseValue(PFAttributeId.HP)) +
                "/" + FormatValue(target.Attributes.GetCurrentValue(PFAttributeId.HP)));
        }

        private void IncreaseMaxHp(CombatUnit unit, string label)
        {
            EnsureDuel();
            unit.Attributes.AddBaseValue(PFAttributeId.MaxHP, 50f);
            TickDuelInternal(0f, 0f);
            AppendLog(label + " MaxHP Base +50，并刷新动态目标/来源修饰器。");
        }

        private void AddHpBase(CombatUnit unit, string label, float delta)
        {
            EnsureDuel();
            unit.Attributes.AddBaseValue(PFAttributeId.HP, delta);
            TickDuelInternal(0f, 0f);
            AppendLog(label + " HP Base " + (delta >= 0f ? "+" : string.Empty) + FormatValue(delta) + ".");
        }

        private static void ResetUnitBaseAttributes(CombatUnit unit)
        {
            unit.Attributes.SetBaseValue(PFAttributeId.MaxHP, 100f);
            unit.Attributes.SetBaseValue(PFAttributeId.HP, 100f);
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
                    "PFGAS Tag 尚未注册。请先初始化 PFGASTagGenerated.RegisterFromLubanTable()，再使用 PFGASSampleScenarioRunner。");
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
            if (lastResults.Count > 18)
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
            GUILayout.Label(label, GUILayout.Width(150f));
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

        private string FormatActiveEffects(CombatUnit unit)
        {
            activeEffectDetails.Clear();
            unit.Effects.GetActiveEffects(activeEffectDetails);
            if (activeEffectDetails.Count == 0)
            {
                return "无";
            }

            var parts = new string[activeEffectDetails.Count];
            for (var i = 0; i < activeEffectDetails.Count; i++)
            {
                var activeEffect = activeEffectDetails[i];
                parts[i] =
                    activeEffect.Effect.EffectId +
                    " x" +
                    activeEffect.StackCount +
                    " (" +
                    FormatRemainingTime(activeEffect) +
                    ")";
            }

            return string.Join(", ", parts);
        }

        private static string FormatRemainingTime(ActiveGameplayEffect activeEffect)
        {
            return activeEffect.IsInfinite
                ? "无限"
                : FormatValue(activeEffect.RemainingTime) + " 秒";
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

