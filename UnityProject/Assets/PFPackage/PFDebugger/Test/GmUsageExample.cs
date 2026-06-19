using System.Reflection;
using UnityEngine;

namespace PFDebugger
{
    /// <summary>
    /// Reference examples for the supported GM registration styles.
    /// Attach this component only if you want to try the manual registration examples at runtime.
    /// 
    /// Supported patterns shown in this file:
    /// 1. Attribute-based static commands via [GmCommand]
    /// 2. Manual registration of static Action commands
    /// 3. Manual registration of instance Action commands
    /// 4. Advanced registration via MethodInfo
    /// 5. Matching UnregisterCommand usage
    /// 
    /// Input syntax examples:
    /// - gmexample.attr.ping
    /// - gmexample.attr.echo [hello world]
    /// - gmexample.manual.instance [42]
    /// - gmexample.methodinfo [Beta]
    /// </summary>
    public sealed class GmUsageExample : MonoBehaviour
    {
        private enum ExampleMode
        {
            Alpha = 0,
            Beta = 1,
            Gamma = 2,
        }

        private sealed class ManualInstanceHost
        {
            public void SetValue(int value)
            {
                Debug.Log($"[GmUsageExample] manual instance value => {value}");
            }
        }

        [Header("Manual Registration")]
        [SerializeField] private bool registerManualExamplesOnStart = true;

        private GmManager gmManager;
        private ManualInstanceHost manualInstanceHost;
        private MethodInfo methodInfoMethod;

        private void Start()
        {
            gmManager = Debugger.GmManager;
            manualInstanceHost = new ManualInstanceHost();

            if (!registerManualExamplesOnStart || gmManager == null)
                return;

            RegisterManualExamples();
        }

        private void OnDestroy()
        {
            if (gmManager == null)
                return;

            UnregisterManualExamples();
        }

        [ContextMenu("Register Manual GM Examples")]
        private void RegisterManualExamples()
        {
            if (gmManager == null)
                gmManager = Debugger.GmManager;

            if (gmManager == null)
            {
                Debug.LogError("[GmUsageExample] GmManager is null.");
                return;
            }

            // 1. Manual registration of a static no-arg command.
            gmManager.RegisterCommand("gmexample.manual.static", "Manual static Action registration example.", ManualStaticCommand);

            // 2. Manual registration of an instance one-arg command.
            // Explicit generic parameter is the safest way for method-group inference with Action<T>.
            gmManager.RegisterCommand<int>("gmexample.manual.instance", "Manual instance Action<int> registration example.",
                manualInstanceHost.SetValue, "value");

            // 3. Advanced registration through MethodInfo.
            methodInfoMethod ??= typeof(GmUsageExample).GetMethod(nameof(MethodInfoExampleCommand),
                BindingFlags.Static | BindingFlags.NonPublic);
            gmManager.RegisterCommand("gmexample.methodinfo", "MethodInfo registration example.",
                methodInfoMethod, null, "mode");
        }

        [ContextMenu("Unregister Manual GM Examples")]
        private void UnregisterManualExamples()
        {
            if (gmManager == null)
                return;

            gmManager.UnregisterCommand(ManualStaticCommand);

            if (manualInstanceHost != null)
                gmManager.UnregisterCommand<int>(manualInstanceHost.SetValue);

            if (methodInfoMethod != null)
                gmManager.UnregisterCommand(methodInfoMethod);
        }

        [GmCommand("gmexample.attr.ping", "Attribute-based static command example.")]
        private static void AttributePing()
        {
            Debug.Log("[GmUsageExample] attribute ping invoked");
        }

        [GmCommand("gmexample.attr.echo", "Attribute-based string command example.", "message")]
        private static void AttributeEcho(string message)
        {
            Debug.Log($"[GmUsageExample] attribute echo => {message}");
        }

        [GmCommand("gmexample.attr.mode", "Attribute-based enum command example.", "mode")]
        private static void AttributeMode(ExampleMode mode)
        {
            Debug.Log($"[GmUsageExample] attribute mode => {mode}");
        }

        private static void ManualStaticCommand()
        {
            Debug.Log("[GmUsageExample] manual static command invoked");
        }

        private static void MethodInfoExampleCommand(ExampleMode mode)
        {
            Debug.Log($"[GmUsageExample] method info command => {mode}");
        }
    }
}
