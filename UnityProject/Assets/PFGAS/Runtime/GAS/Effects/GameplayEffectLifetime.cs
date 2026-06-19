namespace PFGAS.Runtime
{
    /// <summary>GameplayEffect 的持续时间、周期和首次周期执行配置。</summary>
    public readonly struct GameplayEffectLifetime
    {
        public GameplayEffectLifetime(
            GameplayEffectDurationPolicy policy,
            float duration = 0f,
            float period = 0f,
            bool executePeriodicOnApply = false)
        {
            GASGuard.Finite(duration, nameof(duration), "Effect duration must be finite.");
            GASGuard.Finite(period, nameof(period), "Effect period must be finite.");
            GASGuard.NonNegative(period, nameof(period), "Effect period cannot be negative.");

            switch (policy)
            {
                case GameplayEffectDurationPolicy.Instant:
                    GASGuard.NonNegative(duration, nameof(duration), "Instant duration cannot be negative.");

                    duration = 0f;
                    break;
                case GameplayEffectDurationPolicy.Duration:
                    GASGuard.Positive(duration, nameof(duration), "Duration effects require positive duration.");

                    break;
                case GameplayEffectDurationPolicy.Infinite:
                    duration = 0f;
                    break;
            }

            Policy = policy;
            Duration = duration;
            Period = period;
            ExecutePeriodicOnApply = executePeriodicOnApply;
        }

        /// <summary>创建立即结算且不生成 ActiveGameplayEffect 的生命周期。</summary>
        public static GameplayEffectLifetime Instant =>
            new GameplayEffectLifetime(GameplayEffectDurationPolicy.Instant);

        /// <summary>创建持续指定时间的生命周期。</summary>
        public static GameplayEffectLifetime ForDuration(
            float duration,
            float period = 0f,
            bool executePeriodicOnApply = false)
        {
            return new GameplayEffectLifetime(
                GameplayEffectDurationPolicy.Duration,
                duration,
                period,
                executePeriodicOnApply);
        }

        /// <summary>创建无限持续的生命周期。</summary>
        public static GameplayEffectLifetime Infinite(
            float period = 0f,
            bool executePeriodicOnApply = false)
        {
            return new GameplayEffectLifetime(
                GameplayEffectDurationPolicy.Infinite,
                0f,
                period,
                executePeriodicOnApply);
        }

        public GameplayEffectDurationPolicy Policy { get; }

        public float Duration { get; }

        public float Period { get; }

        public bool ExecutePeriodicOnApply { get; }

        public bool HasPeriod => Period > 0f;
    }
}
