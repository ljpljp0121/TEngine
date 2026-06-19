using UnityEngine;

namespace PFGAS.Runtime
{
    public class CombatUnit : MonoBehaviour
    {
        private bool ready;

        public AttributeGraph Attributes { get; private set; }

        public GameplayTagAggregator Tags { get; private set; }

        public GameplayEffectContainer Effects { get; private set; }

        public AbilityContainer AbilityContainer { get; private set; }

        public GameplayEventBus GameplayEventBus { get; private set; }

        protected void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            Init();
            GAS.I.Register(this);
            Enable();
        }

        private void OnDisable()
        {
            Disable();
            GAS.I.Unregister(this);
            Dispose();
        }

        public void EnsureInitialized()
        {
            Init();
        }

        private void Init()
        {
            if (ready)
                return;
            Attributes = new AttributeGraph();
            Tags = new GameplayTagAggregator();
            Effects = new GameplayEffectContainer(this);
            AbilityContainer = new AbilityContainer(this);
            GameplayEventBus = new GameplayEventBus();
            ready = true;
        }

        private void Dispose()
        {
            Effects?.RemoveAll();
            AbilityContainer?.CancelAll();
            Tags?.Clear();
            GameplayEventBus?.Clear();
        }

        public virtual void Enable() { }

        public virtual void Disable() { }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            Effects?.Tick(deltaTime);
            AbilityContainer?.Tick(deltaTime, unscaledDeltaTime);
        }
    }
}
