using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>PFGAS 运行时通用小工具，集中放置跨模块复用的基础判断。</summary>
    public static class PFGASHelper
    {
        public const float ValueEpsilon = 0.000001f;

        /// <summary>判断浮点数是否是有限值。</summary>
        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>判断两个浮点值是否在统一容差内近似相等。</summary>
        public static bool IsNearlyEqual(float a, float b)
        {
            return Math.Abs(a - b) < ValueEpsilon;
        }

        /// <summary>判断两个浮点值是否产生了需要对外感知的变化。</summary>
        public static bool HasMeaningfulChange(float a, float b)
        {
            return !IsNearlyEqual(a, b);
        }

        /// <summary>判断浮点值是否接近 0。</summary>
        public static bool IsNearlyZero(float value)
        {
            return Math.Abs(value) < ValueEpsilon;
        }

        internal static void AddRangeUnique<T>(
            List<T> list,
            IReadOnlyList<T> values)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (!list.Contains(values[i]))
                {
                    list.Add(values[i]);
                }
            }
        }
    }
}
