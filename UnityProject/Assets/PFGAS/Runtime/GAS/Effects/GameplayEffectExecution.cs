namespace PFGAS.Runtime
{
    /// <summary>Execution 执行时可读取的上下文。</summary>
    public readonly struct GameplayEffectExecutionContext
    {
        public GameplayEffectExecutionContext(
            GameplayEffectSpec spec,
            ActiveGameplayEffect activeEffect,
            GameplayEffectExecutionPhase phase)
        {
            Spec = spec;
            ActiveEffect = activeEffect;
            Phase = phase;
        }

        public GameplayEffectSpec Spec { get; }

        public ActiveGameplayEffect ActiveEffect { get; }

        public GameplayEffectExecutionPhase Phase { get; }

        public CombatUnit Source => Spec.Source;

        public CombatUnit Target => Spec.Target;

        public int StackCount => ActiveEffect != null ? ActiveEffect.StackCount : Spec.StackCount;

        public object Payload => Spec.Payload;
    }

    /// <summary>GameplayEffect 生命周期回调的执行逻辑。</summary>
    public interface IGameplayEffectExecution
    {
        GASResult Execute(GameplayEffectExecutionContext context);
    }

    /// <summary>绑定 Execution 实例和它触发的生命周期阶段。</summary>
    public readonly struct GameplayEffectExecutionSpec
    {
        public GameplayEffectExecutionSpec(
            GameplayEffectExecutionPhase phase,
            IGameplayEffectExecution execution)
        {
            Phase = phase;
            Execution = execution;
        }

        public GameplayEffectExecutionPhase Phase { get; }

        public IGameplayEffectExecution Execution { get; }
    }
}
