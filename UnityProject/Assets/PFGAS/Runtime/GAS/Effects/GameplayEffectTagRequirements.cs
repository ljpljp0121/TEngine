using System;
using System.Collections.Generic;
using System.Linq;

namespace PFGAS.Runtime
{
    /// <summary>GameplayEffect 应用前对 Source 和 Target Tag 的要求。</summary>
    public readonly struct GameplayEffectTagRequirements
    {
        public GameplayEffectTagRequirements(
            IEnumerable<PFTagId> requiredSourceTags = null,
            IEnumerable<PFTagId> blockedSourceTags = null,
            IEnumerable<PFTagId> requiredTargetTags = null,
            IEnumerable<PFTagId> blockedTargetTags = null)
        {
            RequiredSourceTags = CopyOrEmpty(requiredSourceTags);
            BlockedSourceTags = CopyOrEmpty(blockedSourceTags);
            RequiredTargetTags = CopyOrEmpty(requiredTargetTags);
            BlockedTargetTags = CopyOrEmpty(blockedTargetTags);
        }

        /// <summary>不要求也不阻挡任何 Tag。</summary>
        public static GameplayEffectTagRequirements None =>
            new GameplayEffectTagRequirements();

        public IReadOnlyList<PFTagId> RequiredSourceTags { get; }

        public IReadOnlyList<PFTagId> BlockedSourceTags { get; }

        public IReadOnlyList<PFTagId> RequiredTargetTags { get; }

        public IReadOnlyList<PFTagId> BlockedTargetTags { get; }

        internal GameplayEffectTagRequirements Normalized()
        {
            return new GameplayEffectTagRequirements(
                RequiredSourceTags,
                BlockedSourceTags,
                RequiredTargetTags,
                BlockedTargetTags);
        }

        private static T[] CopyOrEmpty<T>(IEnumerable<T> values)
        {
            if (values == null)
            {
                return Array.Empty<T>();
            }

            return values.ToArray();
        }
    }
}
