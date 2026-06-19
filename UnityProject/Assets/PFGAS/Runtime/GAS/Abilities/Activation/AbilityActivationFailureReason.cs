namespace PFGAS.Runtime
{
    /// <summary>
    /// Ability 激活失败的预期原因。
    /// </summary>
    public enum AbilityActivationFailureReason
    {
        None = 0,
        AbilityNotFound = 1,
        Disabled = 2,
        AlreadyActive = 3,
    }

    /// <summary>
    /// 表示一次 Ability 激活成功或失败的结果。
    /// </summary>
    public readonly struct AbilityActivationResult
    {
        private AbilityActivationResult(
            bool succeeded,
            AbilityExecution execution,
            AbilityActivationFailureReason failureReason,
            string message)
        {
            Succeeded = succeeded;
            Execution = execution;
            FailureReason = succeeded ? AbilityActivationFailureReason.None : failureReason;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }

        public bool Failed => !Succeeded;

        public AbilityExecution Execution { get; }

        public AbilityActivationFailureReason FailureReason { get; }

        public string Message { get; }

        public static AbilityActivationResult Activated(AbilityExecution execution)
        {
            if (execution == null)
            {
                throw new System.ArgumentNullException(nameof(execution));
            }

            return new AbilityActivationResult(
                true,
                execution,
                AbilityActivationFailureReason.None,
                string.Empty);
        }

        public static AbilityActivationResult Failure(
            AbilityActivationFailureReason failureReason,
            string message = null)
        {
            if (failureReason == AbilityActivationFailureReason.None)
            {
                throw new System.ArgumentException("Failure reason must not be None.", nameof(failureReason));
            }

            return new AbilityActivationResult(
                false,
                null,
                failureReason,
                message);
        }
    }
}
