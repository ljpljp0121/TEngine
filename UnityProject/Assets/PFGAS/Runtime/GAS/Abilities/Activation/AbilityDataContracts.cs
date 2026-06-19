namespace PFGAS.Runtime
{
    /// <summary>
    /// Ability 激活参数的强类型标记契约。
    /// </summary>
    public interface IAbilityArguments
    {
    }

    /// <summary>
    /// Ability 激活目标数据的强类型标记契约。
    /// </summary>
    public interface IAbilityTargetData
    {
    }

    /// <summary>
    /// 暴露主要目标 CombatUnit 的可选目标数据契约。
    /// </summary>
    public interface IPrimaryAbilityTargetData : IAbilityTargetData
    {
        CombatUnit PrimaryTarget { get; }
    }

    /// <summary>
    /// Gameplay Event 负载的强类型标记契约。
    /// </summary>
    public interface IGameplayEventPayload
    {
    }

    /// <summary>
    /// 不需要激活参数的 Ability 使用的显式空参数值。
    /// </summary>
    public readonly struct EmptyAbilityArguments : IAbilityArguments
    {
        public static readonly EmptyAbilityArguments Value = default;
    }

    /// <summary>
    /// 不需要目标数据的 Ability 使用的显式空目标值。
    /// </summary>
    public readonly struct EmptyAbilityTargetData : IAbilityTargetData
    {
        public static readonly EmptyAbilityTargetData Value = default;
    }

    /// <summary>
    /// 不携带负载的 Gameplay Event 使用的显式空负载值。
    /// </summary>
    public readonly struct EmptyGameplayEventPayload : IGameplayEventPayload
    {
        public static readonly EmptyGameplayEventPayload Value = default;
    }

    /// <summary>
    /// 表示单个主要 CombatUnit 目标的目标数据。
    /// </summary>
    public readonly struct CombatUnitTargetData : IPrimaryAbilityTargetData
    {
        public CombatUnitTargetData(CombatUnit primaryTarget)
        {
            PrimaryTarget = primaryTarget;
        }

        public CombatUnit PrimaryTarget { get; }

        public bool HasPrimaryTarget => PrimaryTarget != null;
    }
}
