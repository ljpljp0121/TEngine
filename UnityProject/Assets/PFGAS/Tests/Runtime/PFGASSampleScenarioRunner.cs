using System;
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

        private PFGASSampleUnitFactory duelFactory;
        private CombatUnit unitA;
        private CombatUnit unitB;
        private PFGASSampleLifecycleCounters unitACounters = new PFGASSampleLifecycleCounters();
        private PFGASSampleLifecycleCounters unitBCounters = new PFGASSampleLifecycleCounters();

        public IReadOnlyList<string> LastResults => lastResults;

        public CombatUnit UnitA => unitA;

        public CombatUnit UnitB => unitB;

        public PFGASSampleLifecycleCounters UnitACounters => unitACounters;

        public PFGASSampleLifecycleCounters UnitBCounters => unitBCounters;

        private void Start()
        {
            if (runOnStart)
            {
                ResetDuel();
            }
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

            GUILayout.BeginArea(new Rect(16f, 16f, 390f, Screen.height - 32f), "PFGAS 双人技能样例", GUI.skin.window);
            GUILayout.Label(FormatUnit("A", unitA, unitACounters));
            GUILayout.Label(FormatUnit("B", unitB, unitBCounters));

            GUILayout.Space(6f);
            DrawButtonRow(("重置", ResetDuel), ("Tick +" + tickSeconds + "s", TickDuel), ("清理效果", RemoveAllEffects));
            DrawButtonRow(("A 灼烧 B", CastBurningAToB), ("B 灼烧 A", CastBurningBToA));
            DrawButtonRow(("A 毒 B", CastPoisonAToB), ("B 毒 A", CastPoisonBToA));
            DrawButtonRow(("A 光环 B", CastLeadershipAuraAToB), ("B 光环 A", CastLeadershipAuraBToA));
            DrawButtonRow(("A 开护盾", CastShieldA), ("B 开护盾", CastShieldB));
            DrawButtonRow(("A 事件监听", ActivateLifecycleA), ("B 事件监听", ActivateLifecycleB));
            DrawButtonRow(("A 命中 B", PublishHitAToB), ("B 命中 A", PublishHitBToA));
            DrawButtonRow(("A MaxHP +50", IncreaseAMaxHp), ("B MaxHP +50", IncreaseBMaxHp));

            GUILayout.Space(6f);
            GUILayout.Label("最近操作");
            for (var i = lastResults.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(lastResults[i]);
            }

            GUILayout.EndArea();
        }

        [ContextMenu("Reset PFGAS Duel")]
        public void ResetDuel()
        {
            CleanupDuel();
            duelFactory = new PFGASSampleUnitFactory();
            unitA = duelFactory.CreateUnit("PFGAS Sample Unit A");
            unitB = duelFactory.CreateUnit("PFGAS Sample Unit B");
            unitA.transform.position = new Vector3(-1.5f, 0f, 0f);
            unitB.transform.position = new Vector3(1.5f, 0f, 0f);
            unitACounters = new PFGASSampleLifecycleCounters();
            unitBCounters = new PFGASSampleLifecycleCounters();
            lastResults.Clear();
            AppendLog("重置双人样例：A 与 B 已创建。");
        }

        [ContextMenu("Run PFGAS Samples")]
        public void RunAll()
        {
            lastResults.Clear();
            var burning = PFGASSamples.RunBurningDot();
            var aura = PFGASSamples.RunLeadershipAura();
            var poison = PFGASSamples.RunStackingPoison();
            var shield = PFGASSamples.RunTargetLocalShield();
            var lifecycle = PFGASSamples.RunLifecycleEvent();

            lastResults.Add("灼烧DoT：HP " + burning.HpAfterApply + " -> " + burning.HpAfterExpiry);
            lastResults.Add("队长光环：目标 MaxHP " + aura.FrontInitialMaxHp + " -> " + aura.FrontAfterSourceFlush);
            lastResults.Add("毒层叠加：Stack " + poison.SourceAStackCount + "，HP " + poison.HpAfterBothSourcesPeriod);
            lastResults.Add("生命护盾：HP " + shield.HpAfterApply + " -> " + shield.HpAfterMaxHpChange);
            lastResults.Add("生命周期事件：Event " + lifecycle.EventCountAfterCleanup + "，Active " + lifecycle.ActiveEffectCountAfterCleanup);

            Debug.Log("PFGAS samples finished. See LastResults on this component.", this);
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
            ApplyEffect(unitA, unitB, PFGASSampleEffects.CreateLeadershipAura(), "A 的队长光环加到 B");
        }

        public void CastLeadershipAuraBToA()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitA, PFGASSampleEffects.CreateLeadershipAura(), "B 的队长光环加到 A");
        }

        public void CastShieldA()
        {
            EnsureDuel();
            ApplyEffect(unitA, unitA, PFGASSampleEffects.CreateTargetLocalShield(), "A 为自己开启生命护盾");
        }

        public void CastShieldB()
        {
            EnsureDuel();
            ApplyEffect(unitB, unitB, PFGASSampleEffects.CreateTargetLocalShield(), "B 为自己开启生命护盾");
        }

        public void ActivateLifecycleA()
        {
            EnsureDuel();
            ApplyEffect(
                unitA,
                unitA,
                PFGASSampleEffects.CreatePersistentLifecycleEvent(unitACounters),
                "A 开启生命周期事件监听");
        }

        public void ActivateLifecycleB()
        {
            EnsureDuel();
            ApplyEffect(
                unitB,
                unitB,
                PFGASSampleEffects.CreatePersistentLifecycleEvent(unitBCounters),
                "B 开启生命周期事件监听");
        }

        public void PublishHitAToB()
        {
            EnsureDuel();
            unitB.GameplayEventBus.Publish(PFGASSamples.LifecycleEventName, unitA, unitB);
            AppendLog("A 命中 B：B 事件计数 = " + unitBCounters.EventCount);
        }

        public void PublishHitBToA()
        {
            EnsureDuel();
            unitA.GameplayEventBus.Publish(PFGASSamples.LifecycleEventName, unitB, unitA);
            AppendLog("B 命中 A：A 事件计数 = " + unitACounters.EventCount);
        }

        public void TickDuel()
        {
            EnsureDuel();
            unitA.Tick(tickSeconds, tickSeconds);
            unitB.Tick(tickSeconds, tickSeconds);
            AppendLog("推进时间 " + tickSeconds + " 秒。");
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
            AppendLog("清理 A/B 所有 ActiveGameplayEffect。");
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

            AppendLog(description + " 成功，Handle=" + result.Value.Handle.Value);
        }

        private void IncreaseMaxHp(CombatUnit unit, string label)
        {
            EnsureDuel();
            unit.Attributes.AddBaseValue(PFAttributeId.MaxHP, 50f);
            unitA.Effects.Tick(0f);
            unitB.Effects.Tick(0f);
            AppendLog(label + " MaxHP +50，并刷新动态 Source modifier。");
        }

        private void EnsureDuel()
        {
            if (unitA == null || unitB == null)
            {
                ResetDuel();
            }
        }

        private void AppendLog(string message)
        {
            lastResults.Add(message);
            if (lastResults.Count > 10)
            {
                lastResults.RemoveAt(0);
            }
        }

        private static string FormatUnit(
            string label,
            CombatUnit unit,
            PFGASSampleLifecycleCounters counters)
        {
            if (unit == null)
            {
                return label + "：未创建";
            }

            return label +
                   " HP=" + unit.Attributes.GetCurrentValue(PFAttributeId.HP).ToString("0.##") +
                   " / MaxHP=" + unit.Attributes.GetCurrentValue(PFAttributeId.MaxHP).ToString("0.##") +
                   " Active=" + unit.Effects.ActiveEffectCount +
                   " Tags=" + FormatTags(unit) +
                   " Event=" + counters.EventCount;
        }

        private static string FormatTags(CombatUnit unit)
        {
            var tags = unit.Tags.GetTagsSnapshot();
            return tags.Length == 0 ? "无" : string.Join(",", tags);
        }

        private static void DrawButtonRow(
            (string Text, Action Action) first,
            (string Text, Action Action) second,
            (string Text, Action Action) third = default)
        {
            GUILayout.BeginHorizontal();
            DrawButton(first);
            DrawButton(second);
            if (third.Action != null)
            {
                DrawButton(third);
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawButton((string Text, Action Action) button)
        {
            if (GUILayout.Button(button.Text, GUILayout.Height(28f)))
            {
                button.Action();
            }
        }
    }
}
