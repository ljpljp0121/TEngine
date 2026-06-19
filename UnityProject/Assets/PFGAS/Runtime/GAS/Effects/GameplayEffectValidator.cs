using System.Collections.Generic;

namespace PFGAS.Runtime
{
    internal static class GameplayEffectValidator
    {
        public static GASResult ValidateContext(CombatUnit owner, CombatUnit target)
        {
            if (target == null)
            {
                return GASResult.Fail("MissingTarget", "GameplayEffect requires a target CombatUnit.");
            }

            if (!ReferenceEquals(target, owner))
            {
                return GASResult.Fail("TargetMismatch", "GameplayEffect target must match its owning container.");
            }

            if (owner.Attributes == null)
            {
                return GASResult.Fail("MissingTargetAttributes", "Target CombatUnit has no AttributeGraph.");
            }

            if (owner.Tags == null)
            {
                return GASResult.Fail("MissingTargetTags", "Target CombatUnit has no GameplayTagAggregator.");
            }

            return GASResult.Success();
        }

        public static void ValidateEffectConfiguration(GameplayEffect effect)
        {
            var hasPeriodic = false;
            for (var i = 0; i < effect.Modifiers.Count; i++)
            {
                var modifier = effect.Modifiers[i];
                switch (modifier.Phase)
                {
                    case GameplayEffectModifierPhase.Instant:
                        break;
                    case GameplayEffectModifierPhase.Ongoing:
                        if (effect.Lifetime.Policy == GameplayEffectDurationPolicy.Instant)
                        {
                            GASGuard.ThrowInvalidOperation("Instant GameplayEffects cannot contain Ongoing modifiers.");
                        }

                        break;
                    case GameplayEffectModifierPhase.Periodic:
                        hasPeriodic = true;
                        if (effect.Lifetime.Policy == GameplayEffectDurationPolicy.Instant)
                        {
                            GASGuard.ThrowInvalidOperation("Instant GameplayEffects cannot contain Periodic modifiers.");
                        }

                        break;
                }
            }

            if (hasPeriodic && !effect.Lifetime.HasPeriod)
            {
                GASGuard.ThrowInvalidOperation("Periodic GameplayEffect modifiers require a positive Period.");
            }

            if (effect.Lifetime.Policy == GameplayEffectDurationPolicy.Instant)
            {
                if (effect.GrantedTags.Count > 0)
                {
                    GASGuard.ThrowInvalidOperation("Instant GameplayEffects cannot grant persistent tags.");
                }

                if (effect.Triggers.Count > 0)
                {
                    GASGuard.ThrowInvalidOperation("Instant GameplayEffects cannot register Triggers.");
                }
            }
        }

        public static GASResult ValidateTags(
            GameplayEffect effect,
            CombatUnit source,
            CombatUnit target)
        {
            var tags = effect.Tags;
            if (tags.RequiredSourceTags.Count > 0 && source == null)
            {
                return GASResult.Fail("MissingSource", "GameplayEffect requires source tags but has no source.");
            }

            if (source != null)
            {
                if (source.Tags == null)
                {
                    return GASResult.Fail("MissingSourceTags", "Source CombatUnit has no GameplayTagAggregator.");
                }

                if (!HasAllTags(source.Tags, tags.RequiredSourceTags))
                {
                    return GASResult.Fail("MissingRequiredSourceTags", "Source tags do not satisfy GameplayEffect requirements.");
                }

                if (HasAnyTags(source.Tags, tags.BlockedSourceTags))
                {
                    return GASResult.Fail("BlockedSourceTags", "Source tags block GameplayEffect application.");
                }
            }

            if (!HasAllTags(target.Tags, tags.RequiredTargetTags))
            {
                return GASResult.Fail("MissingRequiredTargetTags", "Target tags do not satisfy GameplayEffect requirements.");
            }

            if (HasAnyTags(target.Tags, tags.BlockedTargetTags))
            {
                return GASResult.Fail("BlockedTargetTags", "Target tags block GameplayEffect application.");
            }

            return GASResult.Success();
        }

        private static bool HasAllTags(
            GameplayTagAggregator tags,
            IReadOnlyList<PFTagId> requiredTags)
        {
            if (requiredTags == null || requiredTags.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < requiredTags.Count; i++)
            {
                if (!tags.HasTag(requiredTags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasAnyTags(
            GameplayTagAggregator tags,
            IReadOnlyList<PFTagId> blockedTags)
        {
            if (blockedTags == null || blockedTags.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < blockedTags.Count; i++)
            {
                if (tags.HasTag(blockedTags[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
