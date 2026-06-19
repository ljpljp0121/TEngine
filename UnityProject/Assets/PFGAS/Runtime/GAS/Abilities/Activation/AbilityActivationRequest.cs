using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 描述一次 Ability 激活所携带的参数与目标数据。
    /// </summary>
    public interface IAbilityActivationRequest
    {
        Type ArgumentsType { get; }

        Type TargetDataType { get; }

        bool TryGetPrimaryTarget(out CombatUnit target);
    }

    /// <summary>
    /// 保存强类型 Ability 激活参数与目标数据的请求值。
    /// </summary>
    public readonly struct AbilityActivationRequest<TArguments, TTargetData> :
        IAbilityActivationRequest
        where TArguments : IAbilityArguments
        where TTargetData : IAbilityTargetData
    {
        public AbilityActivationRequest(TArguments arguments, TTargetData targetData)
        {
            Arguments = arguments;
            TargetData = targetData;
        }

        public TArguments Arguments { get; }

        public TTargetData TargetData { get; }

        public Type ArgumentsType => typeof(TArguments);

        public Type TargetDataType => typeof(TTargetData);

        public bool TryGetPrimaryTarget(out CombatUnit target)
        {
            if (TargetData is IPrimaryAbilityTargetData primaryTargetData)
            {
                target = primaryTargetData.PrimaryTarget;
                return target != null;
            }

            target = null;
            return false;
        }
    }

    /// <summary>
    /// 创建常用 Ability 激活请求的辅助入口。
    /// </summary>
    public static class AbilityActivationRequest
    {
        public static readonly AbilityActivationRequest<EmptyAbilityArguments, EmptyAbilityTargetData> Empty =
            new AbilityActivationRequest<EmptyAbilityArguments, EmptyAbilityTargetData>(
                EmptyAbilityArguments.Value,
                EmptyAbilityTargetData.Value);

        public static AbilityActivationRequest<TArguments, TTargetData> Create<TArguments, TTargetData>(
            TArguments arguments,
            TTargetData targetData)
            where TArguments : IAbilityArguments
            where TTargetData : IAbilityTargetData
        {
            return new AbilityActivationRequest<TArguments, TTargetData>(arguments, targetData);
        }

        public static AbilityActivationRequest<EmptyAbilityArguments, CombatUnitTargetData> ForTarget(
            CombatUnit target)
        {
            return new AbilityActivationRequest<EmptyAbilityArguments, CombatUnitTargetData>(
                EmptyAbilityArguments.Value,
                new CombatUnitTargetData(target));
        }
    }
}
