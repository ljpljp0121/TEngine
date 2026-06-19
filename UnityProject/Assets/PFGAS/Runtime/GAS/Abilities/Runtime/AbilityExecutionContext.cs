using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 单次 Ability 激活共享的运行时上下文。
    /// </summary>
    public class AbilityExecutionContext
    {
        internal AbilityExecutionContext(
            CombatUnit owner,
            IAbilityActivationRequest activationRequest,
            AbilitySpec sourceAbilitySpec = null,
            AbilityExecution execution = null)
        {
            Owner = owner;
            ActivationRequest = activationRequest ?? AbilityActivationRequest.Empty;
            SourceAbilitySpec = sourceAbilitySpec;
            Execution = execution;
        }

        public CombatUnit Owner { get; }

        public IAbilityActivationRequest ActivationRequest { get; }

        public Type ArgumentsType => ActivationRequest.ArgumentsType;

        public Type TargetDataType => ActivationRequest.TargetDataType;

        public AbilitySpec SourceAbilitySpec { get; }

        public AbilityExecution Execution { get; }

        public bool TryGetPrimaryTarget(out CombatUnit target)
        {
            return ActivationRequest.TryGetPrimaryTarget(out target);
        }

        public bool TryGetActivationRequest<TArguments, TTargetData>(
            out AbilityActivationRequest<TArguments, TTargetData> request)
            where TArguments : IAbilityArguments
            where TTargetData : IAbilityTargetData
        {
            if (ActivationRequest is AbilityActivationRequest<TArguments, TTargetData> typedRequest)
            {
                request = typedRequest;
                return true;
            }

            request = default;
            return false;
        }
    }

    /// <summary>
    /// 携带强类型参数和目标数据的 Ability 执行上下文。
    /// </summary>
    public sealed class AbilityExecutionContext<TArguments, TTargetData> : AbilityExecutionContext
        where TArguments : IAbilityArguments
        where TTargetData : IAbilityTargetData
    {
        internal AbilityExecutionContext(
            CombatUnit owner,
            AbilityActivationRequest<TArguments, TTargetData> activationRequest,
            AbilitySpec sourceAbilitySpec = null,
            AbilityExecution execution = null)
            : base(owner, activationRequest, sourceAbilitySpec, execution)
        {
            Arguments = activationRequest.Arguments;
            TargetData = activationRequest.TargetData;
        }

        public TArguments Arguments { get; }

        public TTargetData TargetData { get; }
    }
}
