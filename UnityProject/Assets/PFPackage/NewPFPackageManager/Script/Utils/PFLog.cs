using UnityEngine;

namespace PFPackage
{
    public class PFLog
    {
        public static void Log(string message)
        {
            Debug.Log($"[PFPackageManager] {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[PFPackageManager] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[PFPackageManager] {message}");
        }
    }
}   