using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 管理 CombatUnit 持有的 Ability 与正在运行的执行实例。
    /// </summary>
    public sealed class AbilityContainer
    {
        private readonly AbilitySpecStore specStore;
        private readonly AbilityActivationEvaluator activationEvaluator;
        private readonly AbilityExecutionStore executionStore;

        public AbilityContainer(CombatUnit owner)
        {
            Owner = owner;
            specStore = new AbilitySpecStore(Owner);
            activationEvaluator = new AbilityActivationEvaluator(Owner);
            executionStore = new AbilityExecutionStore();
        }

        public CombatUnit Owner { get; }

        public IReadOnlyDictionary<string, AbilitySpec> AbilitySpecs => specStore.GetSpecsSnapshot();

        public IReadOnlyList<AbilityExecution> RunningExecutions =>
            executionStore.GetRunningExecutionsSnapshot();

        public AbilityActivationResult LastActivationResult { get; private set; }

        public AbilitySpec Grant(GameplayAbility ability, int level = 1, bool enabled = true)
        {
            return specStore.Grant(ability, level, enabled);
        }

        public bool HasAbility(string abilityName)
        {
            return specStore.HasAbility(abilityName);
        }

        public bool TryGetAbilitySpec(string abilityName, out AbilitySpec spec)
        {
            return specStore.TryGet(abilityName, out spec);
        }

        public bool TryActivate(
            string abilityName,
            out AbilityExecution execution)
        {
            var result = Activate(abilityName);
            execution = result.Execution;
            return result.Succeeded;
        }

        public bool TryActivate<TArguments, TTargetData>(
            string abilityName,
            AbilityActivationRequest<TArguments, TTargetData> request,
            out AbilityExecution execution)
            where TArguments : IAbilityArguments
            where TTargetData : IAbilityTargetData
        {
            var result = Activate(abilityName, request);
            execution = result.Execution;
            return result.Succeeded;
        }

        public bool TryActivate<TArguments, TTargetData>(
            string abilityName,
            TArguments arguments,
            TTargetData targetData,
            out AbilityExecution execution)
            where TArguments : IAbilityArguments
            where TTargetData : IAbilityTargetData
        {
            var result = Activate(abilityName, arguments, targetData);
            execution = result.Execution;
            return result.Succeeded;
        }

        public AbilityActivationResult Activate(string abilityName)
        {
            return Activate(abilityName, AbilityActivationRequest.Empty);
        }

        public AbilityActivationResult Activate<TArguments, TTargetData>(
            string abilityName,
            TArguments arguments,
            TTargetData targetData)
            where TArguments : IAbilityArguments
            where TTargetData : IAbilityTargetData
        {
            return Activate(
                abilityName,
                new AbilityActivationRequest<TArguments, TTargetData>(arguments, targetData));
        }

        public AbilityActivationResult Activate<TArguments, TTargetData>(
            string abilityName,
            AbilityActivationRequest<TArguments, TTargetData> request)
            where TArguments : IAbilityArguments
            where TTargetData : IAbilityTargetData
        {
            specStore.TryGet(abilityName, out var spec);
            var evaluation = activationEvaluator.Evaluate(abilityName, spec, request);
            if (evaluation.Failed)
            {
                return SetLastActivationResult(evaluation.FailureResult);
            }

            var nextExecution = new AbilityExecution(spec, request);
            try
            {
                if (!nextExecution.Start())
                {
                    return SetLastActivationResult(
                        AbilityActivationResult.Failure(
                            spec.Enabled
                                ? AbilityActivationFailureReason.AlreadyActive
                                : AbilityActivationFailureReason.Disabled,
                            "Ability could not start."));
                }

            }
            catch
            {
                if (nextExecution.IsStarted && !nextExecution.IsCompleted)
                {
                    nextExecution.CancelAbility();
                }

                throw;
            }

            executionStore.TrackIfRunning(nextExecution);

            return SetLastActivationResult(AbilityActivationResult.Activated(nextExecution));
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            executionStore.Tick(deltaTime, unscaledDeltaTime);
        }

        public bool Cancel(string abilityName)
        {
            return executionStore.CancelByAbilityName(abilityName);
        }

        public void CancelAll()
        {
            executionStore.CancelAll();
        }

        private AbilityActivationResult SetLastActivationResult(AbilityActivationResult result)
        {
            LastActivationResult = result;
            return result;
        }

    }
}
