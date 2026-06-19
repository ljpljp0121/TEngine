using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 可授予 CombatUnit 的 Ability 静态定义。
    /// </summary>
    [Serializable]
    public abstract class GameplayAbility
    {
        private readonly string name;

        protected GameplayAbility(
            string name,
            Type argumentsType,
            Type targetDataType)
        {
            this.name = string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
            ArgumentsType = ValidateDataType(argumentsType, typeof(IAbilityArguments), nameof(argumentsType));
            TargetDataType = ValidateDataType(targetDataType, typeof(IAbilityTargetData), nameof(targetDataType));
        }

        public string Name => name;

        public Type ArgumentsType { get; }

        public Type TargetDataType { get; }

        public bool AcceptsActivationRequest(IAbilityActivationRequest request)
        {
            if (request == null)
            {
                return false;
            }

            return request.ArgumentsType == ArgumentsType &&
                   request.TargetDataType == TargetDataType;
        }

        public void Activate(AbilityExecutionContext context)
        {
            if (!AcceptsActivationRequest(context.ActivationRequest))
            {
                throw new InvalidOperationException(
                    $"Ability '{Name}' expected activation data '{ArgumentsType.Name}, {TargetDataType.Name}' but received '{context.ArgumentsType.Name}, {context.TargetDataType.Name}'.");
            }

            OnActivate(context);
        }

        internal AbilityExecutionContext CreateExecutionContext(
            AbilityExecution execution,
            IAbilityActivationRequest request)
        {
            if (!AcceptsActivationRequest(request))
            {
                throw new InvalidOperationException(
                    $"Ability '{Name}' cannot create a context for activation data '{request?.ArgumentsType.Name}, {request?.TargetDataType.Name}'.");
            }

            return CreateExecutionContextCore(execution, request);
        }

        protected abstract void OnActivate(AbilityExecutionContext context);

        protected abstract AbilityExecutionContext CreateExecutionContextCore(
            AbilityExecution execution,
            IAbilityActivationRequest request);

        private static Type ValidateDataType(Type type, Type contractType, string parameterName)
        {
            if (!contractType.IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    $"Type '{type.Name}' must implement '{contractType.Name}'.",
                    parameterName);
            }

            return type;
        }
    }

    [Serializable]
    /// <summary>
    /// 带强类型参数和目标数据的 Ability 定义基类。
    /// </summary>
    public abstract class GameplayAbility<TArguments, TTargetData> : GameplayAbility
        where TArguments : IAbilityArguments
        where TTargetData : IAbilityTargetData
    {
        protected GameplayAbility(string name)
            : base(
                name,
                typeof(TArguments),
                typeof(TTargetData))
        {
        }

        protected sealed override void OnActivate(AbilityExecutionContext context)
        {
            if (context is not AbilityExecutionContext<TArguments, TTargetData> typedContext)
            {
                throw new InvalidOperationException(
                    $"Ability '{Name}' received an incompatible execution context.");
            }

            OnActivate(typedContext);
        }

        protected override AbilityExecutionContext CreateExecutionContextCore(
            AbilityExecution execution,
            IAbilityActivationRequest request)
        {
            if (request is not AbilityActivationRequest<TArguments, TTargetData> typedRequest)
            {
                throw new InvalidOperationException(
                    $"Ability '{Name}' received an incompatible activation request.");
            }

            return new AbilityExecutionContext<TArguments, TTargetData>(
                execution.Spec.Owner,
                typedRequest,
                execution.Spec,
                execution);
        }

        protected virtual void OnActivate(AbilityExecutionContext<TArguments, TTargetData> context)
        {
        }
    }

    [Serializable]
    /// <summary>
    /// 使用委托实现激活逻辑的强类型 Ability。
    /// </summary>
    public class DelegateGameplayAbility<TArguments, TTargetData> :
        GameplayAbility<TArguments, TTargetData>
        where TArguments : IAbilityArguments
        where TTargetData : IAbilityTargetData
    {
        private readonly Action<AbilityExecutionContext<TArguments, TTargetData>> activateHandler;

        public DelegateGameplayAbility(
            string name,
            Action<AbilityExecutionContext<TArguments, TTargetData>> activateHandler = null)
            : base(name)
        {
            this.activateHandler = activateHandler;
        }

        protected override void OnActivate(AbilityExecutionContext<TArguments, TTargetData> context)
        {
            activateHandler?.Invoke(context);
        }
    }

    [Serializable]
    /// <summary>
    /// 使用空参数和空目标数据的委托 Ability 快捷类型。
    /// </summary>
    public sealed class DelegateGameplayAbility :
        DelegateGameplayAbility<EmptyAbilityArguments, EmptyAbilityTargetData>
    {
        public DelegateGameplayAbility(
            string name,
            Action<AbilityExecutionContext<EmptyAbilityArguments, EmptyAbilityTargetData>> activateHandler = null)
            : base(
                name,
                activateHandler)
        {
        }
    }
}
