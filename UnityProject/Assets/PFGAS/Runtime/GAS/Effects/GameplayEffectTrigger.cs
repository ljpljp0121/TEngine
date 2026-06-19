namespace PFGAS.Runtime
{
    /// <summary>Trigger 激活和停用时可读取的上下文。</summary>
    public readonly struct GameplayEffectTriggerContext
    {
        public GameplayEffectTriggerContext(GameplayEffectSpec spec, ActiveGameplayEffect activeEffect)
        {
            Spec = spec;
            ActiveEffect = activeEffect;
        }

        public GameplayEffectSpec Spec { get; }

        public ActiveGameplayEffect ActiveEffect { get; }

        public CombatUnit Source => Spec.Source;

        public CombatUnit Target => Spec.Target;

        public int StackCount => ActiveEffect.StackCount;

        public object Payload => Spec.Payload;
    }

    /// <summary>ActiveGameplayEffect 生命周期内额外挂接的激活/停用逻辑。</summary>
    public interface IGameplayEffectTrigger
    {
        GASResult Activate(GameplayEffectTriggerContext context);

        void Deactivate(GameplayEffectTriggerContext context);
    }

    /// <summary>绑定一个需要随 ActiveGameplayEffect 生命周期激活的 Trigger。</summary>
    public readonly struct GameplayEffectTriggerSpec
    {
        public GameplayEffectTriggerSpec(IGameplayEffectTrigger trigger)
        {
            Trigger = trigger;
        }

        public IGameplayEffectTrigger Trigger { get; }
    }
}
