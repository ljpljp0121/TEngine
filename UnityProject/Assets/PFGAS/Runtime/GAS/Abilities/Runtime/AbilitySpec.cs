using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// CombatUnit 持有的单个 Ability 记录。
    /// </summary>
    [Serializable]
    public class AbilitySpec
    {
        private int level;

        public AbilitySpec(CombatUnit owner, GameplayAbility ability, int level = 1, bool enabled = true)
        {
            Owner = owner;
            Ability = ability;
            Enabled = enabled;
            SetLevel(level);
        }

        public CombatUnit Owner { get; }

        public GameplayAbility Ability { get; }

        public int Level => level;

        public bool Enabled { get; set; }

        public bool IsActive { get; private set; }

        public object UserData { get; set; }

        public bool CanActivate => Enabled && !IsActive;

        public void SetLevel(int level)
        {
            this.level = Math.Max(1, level);
        }

        internal void MarkActive()
        {
            IsActive = true;
        }

        internal void MarkInactive()
        {
            IsActive = false;
        }
    }
}
