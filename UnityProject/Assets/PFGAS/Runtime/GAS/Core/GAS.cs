using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace PFGAS.Runtime
{
    /// <summary> 全局 GAS 调度器，负责注册 CombatUnit 并在 Unity 帧循环中 Tick。 </summary>
    public class GAS
    {
        private static GAS instance;
        public static GAS I
        {
            get
            {
                instance ??= new GAS();
                return instance;
            }
        }

        private GAS()
        {
            const int capacity = 1024;
            combatUnits = new List<CombatUnit>(capacity);
            cachedCombatUnits = new List<CombatUnit>(capacity);
            var gasDriver = new GameObject("GAS Driver").AddComponent<GASDriver>();
            Object.DontDestroyOnLoad(gasDriver.gameObject);
            gasDriver.gameObject.SetActive(true);
        }

        private readonly List<CombatUnit> cachedCombatUnits;
        private readonly List<CombatUnit> combatUnits;
        public IReadOnlyList<CombatUnit> CombatUnits => combatUnits.ToArray();


        public void Register(CombatUnit combatUnit)
        {
            if (combatUnit == null || combatUnits.Contains(combatUnit))
            {
                return;
            }
            combatUnits.Add(combatUnit);
        }

        public bool Unregister(CombatUnit combatUnit)
        {
            if (combatUnit == null)
            {
                return false;
            }
            return combatUnits.Remove(combatUnit);
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            Profiler.BeginSample($"{nameof(GAS)}::Tick()");

            try
            {
                cachedCombatUnits.Clear();
                cachedCombatUnits.AddRange(combatUnits);

                foreach (var combatUnit in cachedCombatUnits)
                {
                    combatUnit.Tick(deltaTime, unscaledDeltaTime);
                }
            }
            finally
            {
                cachedCombatUnits.Clear();

                Profiler.EndSample();
            }
        }

        public void Clear()
        {
            foreach (var combatUnit in combatUnits)
            {
                combatUnit.Disable();
            }
            combatUnits.Clear();
        }
    }
}
