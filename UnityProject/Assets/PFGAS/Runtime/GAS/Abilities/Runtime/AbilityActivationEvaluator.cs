using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 评估 Ability 是否满足激活条件但不创建执行实例。
    /// </summary>
    internal sealed class AbilityActivationEvaluator
    {
        private readonly CombatUnit owner;

        public AbilityActivationEvaluator(CombatUnit owner)
        {
            this.owner = owner;
        }

        public AbilityActivationEvaluation Evaluate(
            string abilityName,
            AbilitySpec spec,
            IAbilityActivationRequest activationRequest)
        {
            if (spec == null)
            {
                return AbilityActivationEvaluation.Failure(
                    AbilityActivationResult.Failure(
                        AbilityActivationFailureReason.AbilityNotFound,
                        "Ability was not found on this CombatUnit."));
            }

            if (!spec.Ability.AcceptsActivationRequest(activationRequest))
            {
                throw new InvalidOperationException(
                    $"Ability '{abilityName}' expects activation data '{spec.Ability.ArgumentsType.Name}, {spec.Ability.TargetDataType.Name}' but received '{activationRequest.ArgumentsType.Name}, {activationRequest.TargetDataType.Name}'.");
            }

            if (!spec.Enabled)
            {
                return Failure(
                    AbilityActivationFailureReason.Disabled,
                    "Ability is disabled.");
            }

            if (spec.IsActive)
            {
                return Failure(
                    AbilityActivationFailureReason.AlreadyActive,
                    "Ability is already active.");
            }

            var target = ResolveActivationTarget(activationRequest);
            return AbilityActivationEvaluation.Success(target);
        }

        private CombatUnit ResolveActivationTarget(IAbilityActivationRequest activationRequest)
        {
            if (activationRequest != null &&
                activationRequest.TryGetPrimaryTarget(out var target) &&
                target != null)
            {
                return target;
            }

            return owner;
        }

        private static AbilityActivationEvaluation Failure(
            AbilityActivationFailureReason reason,
            string message)
        {
            return AbilityActivationEvaluation.Failure(
                AbilityActivationResult.Failure(reason, message));
        }
    }
}
