using System.Collections.Generic;

namespace PFGAS.Runtime
{
    internal sealed class GameplayEffectModifierResolver
    {
        private readonly CombatUnit owner;

        public GameplayEffectModifierResolver(CombatUnit owner)
        {
            this.owner = owner;
        }

        public GASResult CaptureSnapshotValues(
            GameplayEffect effect,
            CombatUnit source,
            CombatUnit target,
            GameplayEffectCapturedValues capturedValues)
        {
            for (var i = 0; i < effect.Modifiers.Count; i++)
            {
                var modifier = effect.Modifiers[i];
                if (modifier.CapturePolicy != GameplayEffectCapturePolicy.SnapshotOnApply)
                {
                    continue;
                }

                var magnitude = modifier.Magnitude.EvaluateFixed(source, target);
                if (magnitude.Failed)
                {
                    return GASResult.Fail(magnitude.Failure);
                }

                capturedValues.SetValue(i, magnitude.Value);
            }

            return GASResult.Success();
        }

        public GASResult<ModifierSource> CreateOngoingModifierSource(
            GameplayEffectSpec spec,
            int stackCount)
        {
            var modifiers = ResolveModifiers(spec, GameplayEffectModifierPhase.Ongoing, stackCount);
            if (modifiers.Failed)
            {
                return GASResult<ModifierSource>.Fail(modifiers.Failure);
            }

            if (modifiers.Value.Length == 0)
            {
                return GASResult<ModifierSource>.Success(null);
            }

            return GASResult<ModifierSource>.Success(new ModifierSource(spec.Effect.EffectId, modifiers.Value));
        }

        public GASResult<AttributeModifier[]> ResolveModifiers(
            GameplayEffectSpec spec,
            GameplayEffectModifierPhase phase,
            int stackCount)
        {
            var results = new List<AttributeModifier>();
            for (var i = 0; i < spec.Effect.Modifiers.Count; i++)
            {
                var modifier = spec.Effect.Modifiers[i];
                if (modifier.Phase != phase)
                {
                    continue;
                }

                if (!owner.Attributes.TryGetValue(modifier.AttributeId, out _))
                {
                    return GASResult<AttributeModifier[]>.Fail(
                        "MissingTargetAttribute",
                        $"Target is missing attribute '{modifier.AttributeId}'.");
                }

                var magnitude = ResolveMagnitude(spec, i, modifier, phase, stackCount);
                if (magnitude.Failed)
                {
                    return GASResult<AttributeModifier[]>.Fail(magnitude.Failure);
                }

                results.Add(modifier.ToAttributeModifier(magnitude.Value));
            }

            return GASResult<AttributeModifier[]>.Success(results.ToArray());
        }

        public AttributeModifier[] ResolveModifiersOrThrow(
            GameplayEffectSpec spec,
            GameplayEffectModifierPhase phase,
            int stackCount)
        {
            var result = ResolveModifiers(spec, phase, stackCount);
            if (result.Failed)
            {
                GASGuard.ThrowInvalidOperation(result.Failure);
            }

            return result.Value;
        }

        private GASResult<IAttributeMagnitude> ResolveMagnitude(
            GameplayEffectSpec spec,
            int modifierIndex,
            GameplayEffectModifierSpec modifier,
            GameplayEffectModifierPhase phase,
            int stackCount)
        {
            var stackMultiplier = modifier.ScaleByStackCount ? stackCount : 1;
            if (phase == GameplayEffectModifierPhase.Ongoing &&
                modifier.CapturePolicy == GameplayEffectCapturePolicy.DynamicWhileActive &&
                modifier.Magnitude.IsTargetLocal)
            {
                return GASResult<IAttributeMagnitude>.Success(
                    modifier.Magnitude.CreateTargetMagnitude(stackMultiplier));
            }

            float value;
            if (modifier.CapturePolicy == GameplayEffectCapturePolicy.SnapshotOnApply)
            {
                if (!spec.CapturedValues.TryGetValue(modifierIndex, out value))
                {
                    return GASResult<IAttributeMagnitude>.Fail(
                        "MissingCapturedMagnitude",
                        $"GameplayEffectSpec has no captured value for modifier {modifierIndex}.");
                }
            }
            else
            {
                var evaluated = modifier.Magnitude.EvaluateFixed(spec.Source, spec.Target);
                if (evaluated.Failed)
                {
                    return GASResult<IAttributeMagnitude>.Fail(evaluated.Failure);
                }

                value = evaluated.Value;
            }

            if (modifier.ScaleByStackCount)
            {
                value = modifier.Magnitude.ApplyStack(value, stackMultiplier);
            }

            return GASResult<IAttributeMagnitude>.Success(AttributeMagnitude.Fixed(value));
        }
    }
}
