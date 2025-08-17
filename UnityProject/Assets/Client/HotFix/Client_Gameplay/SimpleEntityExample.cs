using GameLogic;
using TEngine;
using UnityEngine;

namespace Client_Gameplay
{
    /// <summary>
    /// 简单的实体逻辑示例
    /// </summary>
    public class SimplePlayerLogic : EntityLogic
    {
        private SimpleHealthComponent _healthComponent;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 添加组件
            _healthComponent = AddEntityComponent<SimpleHealthComponent>();
            _healthComponent.SetHealth(100);

            Log.Info($"玩家实体显示: ID={Entity.Id}");
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            // 简单的移动控制
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                Vector3 movement = new Vector3(horizontal, 0, vertical) * 5f * elapseSeconds;
                CachedTransform.Translate(movement);
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            Log.Info($"玩家实体隐藏: ID={Entity.Id}");
            base.OnHide(isShutdown, userData);
        }
    }

    /// <summary>
    /// 简单的组件示例
    /// </summary>
    public class SimpleHealthComponent : EntityComponent
    {
        private float _currentHealth;
        private float _maxHealth;

        protected override void OnAttach(EntityLogic entityLogic)
        {
            base.OnAttach(entityLogic);
            _maxHealth = 100f;
            _currentHealth = _maxHealth;
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            // 组件的更新逻辑
        }

        public void SetHealth(float health)
        {
            _maxHealth = health;
            _currentHealth = health;
            Log.Info($"设置生命值: {_currentHealth}/{_maxHealth}");
        }

        public void TakeDamage(float damage)
        {
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            Log.Info($"受到伤害: {damage}, 当前生命值: {_currentHealth}/{_maxHealth}");

            if (_currentHealth <= 0)
            {
                Log.Info("生命值为0，实体死亡");
            }
        }

        public bool IsAlive => _currentHealth > 0;
        public float Health => _currentHealth;
        public float MaxHealth => _maxHealth;
    }
}