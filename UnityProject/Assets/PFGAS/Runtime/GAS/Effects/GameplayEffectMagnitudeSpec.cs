using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>描述 GameplayEffect Modifier 数值来源、线性变换和层数缩放规则。</summary>
    public readonly struct GameplayEffectMagnitudeSpec
    {
        private static readonly PFAttributeId[] EmptyDependencies = Array.Empty<PFAttributeId>();

        private GameplayEffectMagnitudeSpec(
            GameplayEffectMagnitudeSource source,
            PFAttributeId attributeId,
            IAttributeMagnitude targetMagnitude,
            float fixedValue,
            float coefficient,
            float preAdd,
            float postAdd)
        {
            ValidateFinite(fixedValue, nameof(fixedValue));
            ValidateFinite(coefficient, nameof(coefficient));
            ValidateFinite(preAdd, nameof(preAdd));
            ValidateFinite(postAdd, nameof(postAdd));

            Source = source;
            AttributeId = attributeId;
            TargetMagnitude = targetMagnitude;
            FixedValue = fixedValue;
            Coefficient = coefficient;
            PreAdd = preAdd;
            PostAdd = postAdd;
        }

        /// <summary>使用固定数值作为 Modifier Magnitude。</summary>
        public static GameplayEffectMagnitudeSpec Fixed(float value)
        {
            return new GameplayEffectMagnitudeSpec(
                GameplayEffectMagnitudeSource.Fixed,
                default,
                null,
                value,
                1f,
                0f,
                0f);
        }

        /// <summary>读取 Source 的属性当前值作为 Modifier Magnitude。</summary>
        public static GameplayEffectMagnitudeSpec SourceAttribute(
            PFAttributeId attributeId,
            float coefficient = 1f,
            float preAdd = 0f,
            float postAdd = 0f)
        {
            return new GameplayEffectMagnitudeSpec(
                GameplayEffectMagnitudeSource.SourceAttribute,
                attributeId,
                null,
                0f,
                coefficient,
                preAdd,
                postAdd);
        }

        /// <summary>读取 Target 的属性当前值作为 Modifier Magnitude。</summary>
        public static GameplayEffectMagnitudeSpec TargetAttribute(
            PFAttributeId attributeId,
            float coefficient = 1f,
            float preAdd = 0f,
            float postAdd = 0f)
        {
            return new GameplayEffectMagnitudeSpec(
                GameplayEffectMagnitudeSource.TargetAttribute,
                attributeId,
                null,
                0f,
                coefficient,
                preAdd,
                postAdd);
        }

        /// <summary>使用目标 AttributeGraph 中可求值的自定义 Magnitude。</summary>
        public static GameplayEffectMagnitudeSpec FromTargetMagnitude(
            IAttributeMagnitude magnitude,
            float coefficient = 1f,
            float preAdd = 0f,
            float postAdd = 0f)
        {
            return new GameplayEffectMagnitudeSpec(
                GameplayEffectMagnitudeSource.TargetMagnitude,
                default,
                magnitude,
                0f,
                coefficient,
                preAdd,
                postAdd);
        }

        public GameplayEffectMagnitudeSource Source { get; }

        public PFAttributeId AttributeId { get; }

        public IAttributeMagnitude TargetMagnitude { get; }

        public float FixedValue { get; }

        public float Coefficient { get; }

        public float PreAdd { get; }

        public float PostAdd { get; }

        public bool RequiresSource => Source == GameplayEffectMagnitudeSource.SourceAttribute;

        public bool IsTargetLocal =>
            Source == GameplayEffectMagnitudeSource.TargetAttribute ||
            Source == GameplayEffectMagnitudeSource.TargetMagnitude;

        internal IReadOnlyList<PFAttributeId> SourceDependencies =>
            Source == GameplayEffectMagnitudeSource.SourceAttribute
                ? new[] { AttributeId }
                : EmptyDependencies;

        internal GASResult<float> EvaluateFixed(CombatUnit source, CombatUnit target)
        {
            switch (Source)
            {
                case GameplayEffectMagnitudeSource.Fixed:
                    return GASResult<float>.Success(FixedValue);
                case GameplayEffectMagnitudeSource.SourceAttribute:
                    return EvaluateAttribute(source, "Source");
                case GameplayEffectMagnitudeSource.TargetAttribute:
                    return EvaluateAttribute(target, "Target");
                case GameplayEffectMagnitudeSource.TargetMagnitude:
                    return EvaluateTargetMagnitude(target);
            }

            return GASResult<float>.Success(FixedValue);
        }

        internal IAttributeMagnitude CreateTargetMagnitude(int stackCount)
        {
            var coefficient = Coefficient * stackCount;
            var postAdd = (PreAdd * Coefficient + PostAdd) * stackCount;
            ValidateFinite(coefficient, nameof(coefficient));
            ValidateFinite(postAdd, nameof(postAdd));

            switch (Source)
            {
                case GameplayEffectMagnitudeSource.Fixed:
                    return AttributeMagnitude.ScalableFloat(FixedValue, coefficient, postAdd);
                case GameplayEffectMagnitudeSource.TargetAttribute:
                    return AttributeMagnitude.AttributeBased(AttributeId, coefficient, postAdd);
                case GameplayEffectMagnitudeSource.TargetMagnitude:
                    return AttributeMagnitude.Transform(TargetMagnitude, coefficient, postAdd);
                case GameplayEffectMagnitudeSource.SourceAttribute:
                    return GASGuard.ThrowInvalidOperation<IAttributeMagnitude>(
                        "SourceAttribute magnitude cannot enter AttributeGraph as a live dependency.");
            }

            return AttributeMagnitude.ScalableFloat(FixedValue, coefficient, postAdd);
        }

        internal float ApplyStack(float value, int stackCount)
        {
            var result = value * stackCount;
            ValidateFinite(result, nameof(result));
            return result;
        }

        private GASResult<float> EvaluateTargetMagnitude(CombatUnit target)
        {
            if (target == null)
            {
                return GASResult<float>.Fail(
                    "MissingTarget",
                    "Effect magnitude requires target AttributeGraph.");
            }

            if (target.Attributes == null)
            {
                return GASResult<float>.Fail(
                    "MissingTargetAttributes",
                    "Target CombatUnit has no AttributeGraph.");
            }

            for (var i = 0; i < TargetMagnitude.Dependencies.Count; i++)
            {
                if (!target.Attributes.TryGetValue(TargetMagnitude.Dependencies[i], out _))
                {
                    return GASResult<float>.Fail(
                        "MissingAttribute",
                        $"Target is missing attribute '{TargetMagnitude.Dependencies[i]}'.");
                }
            }

            try
            {
                var context = new AttributeGraphContext(target.Attributes);
                var rawValue = TargetMagnitude.Evaluate(context);
                return GASResult<float>.Success(TransformValue(rawValue));
            }
            catch (Exception exception)
            {
                return GASResult<float>.Fail(
                    "MagnitudeEvaluationFailed",
                    exception.Message);
            }
        }

        private GASResult<float> EvaluateAttribute(CombatUnit unit, string label)
        {
            if (unit == null)
            {
                return GASResult<float>.Fail(
                    "MissingAttributeSource",
                    $"Effect magnitude requires {label} attribute '{AttributeId}'.");
            }

            if (unit.Attributes == null || !unit.Attributes.TryGetValue(AttributeId, out _))
            {
                return GASResult<float>.Fail(
                    "MissingAttribute",
                    $"{label} is missing attribute '{AttributeId}'.");
            }

            return GASResult<float>.Success(TransformValue(unit.Attributes.GetCurrentValue(AttributeId)));
        }

        private float TransformValue(float value)
        {
            var result = (value + PreAdd) * Coefficient + PostAdd;
            ValidateFinite(result, nameof(result));
            return result;
        }

        private static void ValidateFinite(float value, string name)
        {
            GASGuard.Finite(value, name, "Effect magnitude value must be finite.");
        }
    }
}
