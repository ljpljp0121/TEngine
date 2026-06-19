using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>组合左右两个 Magnitude 并执行二元运算的表达式节点。</summary>
    internal sealed class BinaryAttributeMagnitude : IAttributeMagnitude
    {
        private readonly BinaryMagnitudeOperation operation;
        private readonly IAttributeMagnitude left;
        private readonly IAttributeMagnitude right;
        private readonly PFAttributeId[] dependencies;

        public BinaryAttributeMagnitude(
            BinaryMagnitudeOperation operation,
            IAttributeMagnitude left,
            IAttributeMagnitude right)
        {
            this.operation = operation;
            this.left = left;
            this.right = right;

            var mergedDependencies = new List<PFAttributeId>();
            PFGASHelper.AddRangeUnique(mergedDependencies, left.Dependencies);
            PFGASHelper.AddRangeUnique(mergedDependencies, right.Dependencies);
            dependencies = mergedDependencies.ToArray();
        }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Evaluate(AttributeGraphContext context)
        {
            var leftValue = left.Evaluate(context);
            var rightValue = right.Evaluate(context);
            var result = leftValue + rightValue;
            switch (operation)
            {
                case BinaryMagnitudeOperation.Add:
                    break;
                case BinaryMagnitudeOperation.Subtract:
                    result = leftValue - rightValue;
                    break;
                case BinaryMagnitudeOperation.Multiply:
                    result = leftValue * rightValue;
                    break;
                case BinaryMagnitudeOperation.Divide:
                    if (PFGASHelper.IsNearlyZero(rightValue))
                    {
                        GASGuard.ThrowInvalidOperation("Attribute magnitude divide denominator cannot be zero.");
                    }

                    result = leftValue / rightValue;
                    break;
                case BinaryMagnitudeOperation.Min:
                    result = Math.Min(leftValue, rightValue);
                    break;
                case BinaryMagnitudeOperation.Max:
                    result = Math.Max(leftValue, rightValue);
                    break;
            }

            return AttributeMagnitude.ValidateFinite(result, operation.ToString());
        }
    }
}
