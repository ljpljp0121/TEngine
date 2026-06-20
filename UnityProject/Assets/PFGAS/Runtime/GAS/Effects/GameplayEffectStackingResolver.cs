using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    internal enum GameplayEffectStackingAction
    {
        CreateNew,
        ReturnExisting,
        RefreshExisting,
        StackExisting,
    }

    internal readonly struct GameplayEffectStackingDecision
    {
        private GameplayEffectStackingDecision(
            GameplayEffectStackingAction action,
            ActiveGameplayEffect existingEffect,
            ActiveGameplayEffect existingToReplace,
            int newStackCount,
            bool refreshTiming)
        {
            Action = action;
            ExistingEffect = existingEffect;
            ExistingToReplace = existingToReplace;
            NewStackCount = newStackCount;
            RefreshTiming = refreshTiming;
        }

        public GameplayEffectStackingAction Action { get; }

        public ActiveGameplayEffect ExistingEffect { get; }

        public ActiveGameplayEffect ExistingToReplace { get; }

        public int NewStackCount { get; }

        public bool RefreshTiming { get; }

        public bool ShouldCreateActiveEffect => Action == GameplayEffectStackingAction.CreateNew;

        public bool ReturnsExistingHandle =>
            Action == GameplayEffectStackingAction.ReturnExisting ||
            Action == GameplayEffectStackingAction.RefreshExisting ||
            Action == GameplayEffectStackingAction.StackExisting;

        public GameplayEffectHandle ReturnHandle =>
            ExistingEffect != null ? ExistingEffect.Handle : GameplayEffectHandle.Invalid;

        public static GameplayEffectStackingDecision CreateNew(ActiveGameplayEffect existingToReplace = null)
        {
            return new GameplayEffectStackingDecision(
                GameplayEffectStackingAction.CreateNew,
                null,
                existingToReplace,
                0,
                false);
        }

        public static GameplayEffectStackingDecision ReturnExisting(ActiveGameplayEffect existing)
        {
            return new GameplayEffectStackingDecision(
                GameplayEffectStackingAction.ReturnExisting,
                existing,
                null,
                existing.StackCount,
                false);
        }

        public static GameplayEffectStackingDecision RefreshExisting(ActiveGameplayEffect existing)
        {
            return new GameplayEffectStackingDecision(
                GameplayEffectStackingAction.RefreshExisting,
                existing,
                null,
                existing.StackCount,
                true);
        }

        public static GameplayEffectStackingDecision StackExisting(
            ActiveGameplayEffect existing,
            int newStackCount,
            bool refreshTiming)
        {
            return new GameplayEffectStackingDecision(
                GameplayEffectStackingAction.StackExisting,
                existing,
                null,
                newStackCount,
                refreshTiming);
        }
    }

    internal sealed class GameplayEffectStackingResolver
    {
        public GASResult<GameplayEffectStackingDecision> Decide(
            GameplayEffectSpec spec,
            IEnumerable<ActiveGameplayEffect> activeEffects)
        {
            var stacking = spec.Effect.Stacking;
            if (stacking.Mode == GameplayEffectStackingMode.Independent)
            {
                return GASResult<GameplayEffectStackingDecision>.Success(
                    GameplayEffectStackingDecision.CreateNew());
            }

            var existing = FindMatchingActiveEffect(spec, activeEffects);
            if (existing == null)
            {
                return GASResult<GameplayEffectStackingDecision>.Success(
                    GameplayEffectStackingDecision.CreateNew());
            }

            switch (stacking.Mode)
            {
                case GameplayEffectStackingMode.Replace:
                    return GASResult<GameplayEffectStackingDecision>.Success(
                        GameplayEffectStackingDecision.CreateNew(existing));
                case GameplayEffectStackingMode.Refresh:
                    return GASResult<GameplayEffectStackingDecision>.Success(
                        GameplayEffectStackingDecision.RefreshExisting(existing));
                case GameplayEffectStackingMode.Stack:
                    return DecideStackMode(spec, existing, stacking);
                case GameplayEffectStackingMode.Independent:
                    return GASResult<GameplayEffectStackingDecision>.Success(
                        GameplayEffectStackingDecision.CreateNew());
            }

            return GASResult<GameplayEffectStackingDecision>.Success(
                GameplayEffectStackingDecision.CreateNew());
        }

        private static GASResult<GameplayEffectStackingDecision> DecideStackMode(
            GameplayEffectSpec spec,
            ActiveGameplayEffect existing,
            GameplayEffectStackingPolicy stacking)
        {
            if (existing.StackCount >= stacking.StackLimit)
            {
                switch (stacking.OverflowPolicy)
                {
                    case GameplayEffectOverflowPolicy.Fail:
                        return GASResult<GameplayEffectStackingDecision>.Fail(
                            "StackLimitReached",
                            "GameplayEffect stack limit has been reached.");
                    case GameplayEffectOverflowPolicy.Ignore:
                        return GASResult<GameplayEffectStackingDecision>.Success(
                            GameplayEffectStackingDecision.ReturnExisting(existing));
                    case GameplayEffectOverflowPolicy.Refresh:
                        return GASResult<GameplayEffectStackingDecision>.Success(
                            GameplayEffectStackingDecision.RefreshExisting(existing));
                    case GameplayEffectOverflowPolicy.ReplaceOldest:
                        return GASResult<GameplayEffectStackingDecision>.Success(
                            GameplayEffectStackingDecision.CreateNew(existing));
                }

                return GASResult<GameplayEffectStackingDecision>.Success(
                    GameplayEffectStackingDecision.ReturnExisting(existing));
            }

            return GASResult<GameplayEffectStackingDecision>.Success(
                GameplayEffectStackingDecision.StackExisting(
                    existing,
                    existing.StackCount + spec.StackCount,
                    stacking.RefreshDurationOnStack));
        }

        private static ActiveGameplayEffect FindMatchingActiveEffect(
            GameplayEffectSpec spec,
            IEnumerable<ActiveGameplayEffect> activeEffects)
        {
            foreach (var activeEffect in activeEffects)
            {
                if (!string.Equals(
                        activeEffect.Effect.EffectId,
                        spec.Effect.EffectId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (spec.Effect.Stacking.Scope == GameplayEffectStackingScope.BySourceAndTarget &&
                    !ReferenceEquals(activeEffect.Source, spec.Source))
                {
                    continue;
                }

                return activeEffect;
            }

            return null;
        }
    }
}
