namespace PFGAS.Runtime
{
    /// <summary>
    /// Ability 激活规则评估的内部结果。
    /// </summary>
    internal readonly struct AbilityActivationEvaluation
    {
        private AbilityActivationEvaluation(
            bool succeeded,
            AbilityActivationResult failureResult,
            CombatUnit target)
        {
            Succeeded = succeeded;
            FailureResult = failureResult;
            Target = target;
        }

        public bool Succeeded { get; }

        public bool Failed => !Succeeded;

        public AbilityActivationResult FailureResult { get; }

        public CombatUnit Target { get; }

        public static AbilityActivationEvaluation Success(CombatUnit target)
        {
            return new AbilityActivationEvaluation(true, default(AbilityActivationResult), target);
        }

        public static AbilityActivationEvaluation Failure(AbilityActivationResult failureResult)
        {
            return new AbilityActivationEvaluation(false, failureResult, null);
        }
    }
}
