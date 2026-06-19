using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PFGAS.Runtime
{
    internal static class GASGuard
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Finite(float value, string paramName, string message)
        {
            if (!PFGASHelper.IsFinite(value))
            {
                ThrowArgument(message, paramName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NonNegative(float value, string paramName, string message)
        {
            ThrowArgumentOutOfRangeIf(value < 0f, paramName, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Positive(float value, string paramName, string message)
        {
            ThrowArgumentOutOfRangeIf(value <= 0f, paramName, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperation(GASFailure failure)
        {
            ThrowInvalidOperation(failure.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TResult ThrowInvalidOperation<TResult>(string message)
        {
            throw new InvalidOperationException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgument(string message, string paramName = null)
        {
            throw new ArgumentException(message, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRange(string paramName, string message)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowKeyNotFound(string message)
        {
            throw new KeyNotFoundException(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowArgumentOutOfRangeIf(bool condition, string paramName, string message)
        {
            if (condition)
            {
                ThrowArgumentOutOfRange(paramName, message);
            }
        }
    }
}
