namespace PFGAS.Runtime
{
    /// <summary>GameplayEffect 的生命周期类型。</summary>
    public enum GameplayEffectDurationPolicy
    {
        Instant,
        Duration,
        Infinite,
    }

    /// <summary>Modifier 在 GameplayEffect 生命周期中的生效阶段。</summary>
    public enum GameplayEffectModifierPhase
    {
        Instant,
        Ongoing,
        Periodic,
    }

    /// <summary>GameplayEffect Modifier 数值的来源。</summary>
    public enum GameplayEffectMagnitudeSource
    {
        Fixed,
        SourceAttribute,
        TargetAttribute,
        TargetMagnitude,
    }

    /// <summary>需要读取属性时，数值捕获和重算的策略。</summary>
    public enum GameplayEffectCapturePolicy
    {
        SnapshotOnApply,
        DynamicWhileActive,
        ReevaluateOnPeriod,
    }

    /// <summary>同类 GameplayEffect 再次应用到目标时的处理模式。</summary>
    public enum GameplayEffectStackingMode
    {
        Independent,
        Replace,
        Refresh,
        Stack,
    }

    /// <summary>Stacking 匹配已有效果时使用的归属范围。</summary>
    public enum GameplayEffectStackingScope
    {
        ByTarget,
        BySourceAndTarget,
    }

    /// <summary>Stack 达到上限后继续应用时的溢出策略。</summary>
    public enum GameplayEffectOverflowPolicy
    {
        Fail,
        Ignore,
        Refresh,
        ReplaceOldest,
    }

    /// <summary>Execution 可挂接的 GameplayEffect 生命周期回调阶段。</summary>
    public enum GameplayEffectExecutionPhase
    {
        OnApply,
        OnPeriod,
        OnRemove,
    }
}
