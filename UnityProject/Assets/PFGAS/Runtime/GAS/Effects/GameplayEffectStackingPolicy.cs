using System;

namespace PFGAS.Runtime
{
    /// <summary>描述 GameplayEffect 重复应用时如何刷新、叠层或替换已有效果。</summary>
    public readonly struct GameplayEffectStackingPolicy
    {
        public GameplayEffectStackingPolicy(
            GameplayEffectStackingMode mode,
            GameplayEffectStackingScope scope = GameplayEffectStackingScope.ByTarget,
            int stackLimit = 1,
            bool refreshDurationOnStack = true,
            GameplayEffectOverflowPolicy overflowPolicy = GameplayEffectOverflowPolicy.Fail)
        {
            GASGuard.NonNegative(stackLimit, nameof(stackLimit), "Stack limit cannot be negative.");
            if (mode == GameplayEffectStackingMode.Stack)
            {
                GASGuard.Positive(stackLimit, nameof(stackLimit), "Stack mode requires a positive stack limit.");
            }

            Mode = mode;
            Scope = scope;
            StackLimit = stackLimit;
            RefreshDurationOnStack = refreshDurationOnStack;
            OverflowPolicy = overflowPolicy;
        }

        /// <summary>每次应用都创建独立 ActiveGameplayEffect。</summary>
        public static GameplayEffectStackingPolicy Independent()
        {
            return new GameplayEffectStackingPolicy(
                GameplayEffectStackingMode.Independent,
                GameplayEffectStackingScope.ByTarget,
                0,
                false,
                GameplayEffectOverflowPolicy.Ignore);
        }

        /// <summary>新效果提交时替换匹配范围内已有的 ActiveGameplayEffect。</summary>
        public static GameplayEffectStackingPolicy Replace(
            GameplayEffectStackingScope scope = GameplayEffectStackingScope.ByTarget)
        {
            return new GameplayEffectStackingPolicy(
                GameplayEffectStackingMode.Replace,
                scope,
                1,
                true,
                GameplayEffectOverflowPolicy.ReplaceOldest);
        }

        /// <summary>再次应用时只刷新已有 ActiveGameplayEffect 的时长。</summary>
        public static GameplayEffectStackingPolicy Refresh(
            GameplayEffectStackingScope scope = GameplayEffectStackingScope.ByTarget)
        {
            return new GameplayEffectStackingPolicy(
                GameplayEffectStackingMode.Refresh,
                scope,
                1,
                true,
                GameplayEffectOverflowPolicy.Refresh);
        }

        /// <summary>再次应用时增加已有 ActiveGameplayEffect 的 StackCount。</summary>
        public static GameplayEffectStackingPolicy Stack(
            int stackLimit,
            GameplayEffectStackingScope scope = GameplayEffectStackingScope.ByTarget,
            bool refreshDurationOnStack = true,
            GameplayEffectOverflowPolicy overflowPolicy = GameplayEffectOverflowPolicy.Fail)
        {
            return new GameplayEffectStackingPolicy(
                GameplayEffectStackingMode.Stack,
                scope,
                stackLimit,
                refreshDurationOnStack,
                overflowPolicy);
        }

        public GameplayEffectStackingMode Mode { get; }

        public GameplayEffectStackingScope Scope { get; }

        public int StackLimit { get; }

        public bool RefreshDurationOnStack { get; }

        public GameplayEffectOverflowPolicy OverflowPolicy { get; }

        internal GameplayEffectStackingPolicy Normalized()
        {
            if (Mode == GameplayEffectStackingMode.Independent && StackLimit == 0)
            {
                return Independent();
            }

            return this;
        }
    }
}
