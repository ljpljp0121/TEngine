using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 管理 CombatUnit 已授予的 Ability Spec 集合。
    /// </summary>
    internal sealed class AbilitySpecStore
    {
        private readonly CombatUnit owner;
        private readonly Dictionary<string, AbilitySpec> specs =
            new Dictionary<string, AbilitySpec>(StringComparer.Ordinal);

        public AbilitySpecStore(CombatUnit owner)
        {
            this.owner = owner;
        }

        public IReadOnlyDictionary<string, AbilitySpec> GetSpecsSnapshot()
        {
            return new Dictionary<string, AbilitySpec>(specs, StringComparer.Ordinal);
        }

        public AbilitySpec Grant(GameplayAbility ability, int level = 1, bool enabled = true)
        {
            if (specs.TryGetValue(ability.Name, out var existingSpec))
            {
                return existingSpec;
            }

            var spec = new AbilitySpec(owner, ability, level, enabled);
            specs.Add(ability.Name, spec);
            return spec;
        }

        public bool HasAbility(string abilityName)
        {
            return !string.IsNullOrEmpty(abilityName) && specs.ContainsKey(abilityName);
        }

        public bool TryGet(string abilityName, out AbilitySpec spec)
        {
            if (string.IsNullOrEmpty(abilityName))
            {
                spec = null;
                return false;
            }

            return specs.TryGetValue(abilityName, out spec);
        }
    }
}
