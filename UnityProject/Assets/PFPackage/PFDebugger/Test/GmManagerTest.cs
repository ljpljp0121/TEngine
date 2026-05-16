using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PFDebugger
{
    /// <summary>
    /// Full runtime test suite for GmManager.
    /// Attach this component to any GameObject, enter play mode, and inspect LogPanel output.
    /// </summary>
    public class GmManagerTest : MonoBehaviour
    {
        private enum TestMode
        {
            Alpha = 0,
            Beta = 1,
            Gamma = 2,
        }

        private sealed class ManualCommandHost
        {
            public void SetNumber(int value)
            {
                manualInstanceValue = value;
                Debug.Log($"{LogPrefix} manual.instance => {value}");
            }
        }

        private const string LogPrefix = "[GmManagerTest]";

        [Header("Auto Run")]
        [SerializeField] private bool runOnStart = true;

        [Header("Manual Input")]
        [SerializeField] private string customCommand = "gmtest.echo [custom message]";

        private GmManager gmManager;
        private LogManager logManager;
        private ManualCommandHost manualCommandHost;

        private int passCount;
        private int failCount;

        private readonly List<GmCommandInfo> suggestionBuffer = new List<GmCommandInfo>(16);

        private static bool pingInvoked;
        private static string echoedMessage;
        private static int addResult;
        private static bool toggleValue;
        private static TestMode modeValue;
        private static bool manualStaticInvoked;
        private static int manualInstanceValue;

        private IEnumerator Start()
        {
            yield return null;

            gmManager = Debugger.GmManager;
            logManager = Debugger.LogManager;
            manualCommandHost = new ManualCommandHost();

            if (runOnStart)
                yield return RunAllTests();
        }

        [ContextMenu("Run All GM Tests")]
        public void RunAllTestsFromContextMenu()
        {
            StartCoroutine(RunAllTests());
        }

        [ContextMenu("Run Custom GM Command")]
        public void RunCustomCommand()
        {
            if (gmManager == null)
                gmManager = Debugger.GmManager;

            GmCommandExecutionResult result = gmManager.ExecuteCommand(customCommand);
            Debug.Log($"{LogPrefix} Custom command '{customCommand}' => success={result.Success}, message={result.Message}");
        }

        [ContextMenu("Log Suggestions For Custom Command")]
        public void LogCustomSuggestions()
        {
            if (gmManager == null)
                gmManager = Debugger.GmManager;

            suggestionBuffer.Clear();
            gmManager.GetSuggestions(customCommand, suggestionBuffer);

            if (suggestionBuffer.Count == 0)
            {
                Debug.Log($"{LogPrefix} Suggestions for '{customCommand}': none");
                return;
            }

            Debug.Log($"{LogPrefix} Suggestions for '{customCommand}': {BuildSuggestionText(suggestionBuffer)}");
        }

        private IEnumerator RunAllTests()
        {
            passCount = 0;
            failCount = 0;

            yield return TestManagerSetup();
            yield return TestAttributeCommandRegistration();
            yield return TestBuiltInHelpCommands();
            yield return TestBasicExecution();
            yield return TestParsingFailures();
            yield return TestSuggestions();
            yield return TestManualRegistration();
            yield return TestUnregister();
            yield return TestRegisterValidation();

            PrintSummary();
        }

        private IEnumerator TestManagerSetup()
        {
            Assert(gmManager != null, "A01: GmManager exists");
            Assert(logManager != null, "A02: LogManager exists");
            yield return PrepareLogContext();
            Assert(gmManager.CommandCount > 0, "A03: At least one GM command is registered");
        }

        private IEnumerator TestAttributeCommandRegistration()
        {
            yield return PrepareLogContext();

            suggestionBuffer.Clear();
            gmManager.GetSuggestions("gmtest", suggestionBuffer);

            Assert(ContainsSuggestion("gmtest.ping"), "B01: Attribute command gmtest.ping registered");
            Assert(ContainsSuggestion("gmtest.echo"), "B02: Attribute command gmtest.echo registered");
            Assert(ContainsSuggestion("gmtest.add"), "B03: Attribute command gmtest.add registered");
            Assert(ContainsSuggestion("gmtest.toggle"), "B04: Attribute command gmtest.toggle registered");
            Assert(ContainsSuggestion("gmtest.mode"), "B05: Attribute command gmtest.mode registered");
        }

        private IEnumerator TestBuiltInHelpCommands()
        {
            yield return PrepareLogContext();

            GmCommandExecutionResult result = gmManager.ExecuteCommand("help");
            Assert(result.Success, "C01: help executes successfully");

            yield return WaitForLogFrames();
            Assert(HasVisibleLogContaining("GM commands:"), "C02: help writes command list into LogManager");

            yield return PrepareLogContext();

            result = gmManager.ExecuteCommand("help [gmtest.echo]");
            Assert(result.Success, "C03: help [command] executes successfully");

            yield return WaitForLogFrames();
            Assert(HasVisibleLogContaining("gmtest.echo [message:string]"), "C04: help [command] prints target signature");
        }

        private IEnumerator TestBasicExecution()
        {
            ResetCommandState();

            yield return PrepareLogContext();
            GmCommandExecutionResult result = gmManager.ExecuteCommand("gmtest.ping");
            Assert(result.Success, "D01: gmtest.ping executes successfully");
            Assert(pingInvoked, "D02: gmtest.ping side effect applied");
            yield return WaitForLogFrames();
            Assert(HasVisibleLogContaining("gmtest.ping invoked"), "D03: gmtest.ping writes to LogPanel");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.echo [hello world]");
            Assert(result.Success, "D04: gmtest.echo executes successfully");
            Assert(echoedMessage == "hello world", "D05: string parameter parsed correctly");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.add [7] [12]");
            Assert(result.Success, "D06: gmtest.add executes successfully");
            Assert(addResult == 19, "D07: int parameters parsed correctly");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.toggle [yes]");
            Assert(result.Success, "D08: gmtest.toggle executes successfully");
            Assert(toggleValue, "D09: bool parameter parsed correctly");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.mode [beta]");
            Assert(result.Success, "D10: gmtest.mode executes successfully");
            Assert(modeValue == TestMode.Beta, "D11: enum parameter parsed case-insensitively");
        }

        private IEnumerator TestParsingFailures()
        {
            yield return PrepareLogContext();

            GmCommandExecutionResult result = gmManager.ExecuteCommand(string.Empty);
            Assert(!result.Success, "E01: empty input fails");
            Assert(ContainsText(result.Message, "empty"), "E02: empty input returns readable error");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.unknown");
            Assert(!result.Success, "E03: unknown command fails");
            Assert(ContainsText(result.Message, "Unknown GM command"), "E04: unknown command returns readable error");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.echo hello");
            Assert(!result.Success, "E05: missing [] syntax fails");
            Assert(ContainsText(result.Message, "wrapped with []"), "E06: missing [] reports parse rule");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.echo [hello");
            Assert(!result.Success, "E07: missing closing bracket fails");
            Assert(ContainsText(result.Message, "closing ']'"), "E08: missing closing bracket reports parse rule");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.add [abc] [2]");
            Assert(!result.Success, "E09: invalid int fails");
            Assert(ContainsText(result.Message, "valid int"), "E10: invalid int returns readable error");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.toggle [maybe]");
            Assert(!result.Success, "E11: invalid bool fails");
            Assert(ContainsText(result.Message, "valid bool"), "E12: invalid bool returns readable error");

            yield return PrepareLogContext();
            result = gmManager.ExecuteCommand("gmtest.add [1]");
            Assert(!result.Success, "E13: wrong argument count fails");
            Assert(ContainsText(result.Message, "Argument count mismatch"), "E14: wrong argument count returns readable error");
        }

        private IEnumerator TestSuggestions()
        {
            yield return PrepareLogContext();

            suggestionBuffer.Clear();
            gmManager.GetSuggestions("gmtest", suggestionBuffer);
            Assert(suggestionBuffer.Count >= 5, "F01: prefix suggestion returns multiple gmtest commands");

            suggestionBuffer.Clear();
            gmManager.GetSuggestions("echo", suggestionBuffer);
            Assert(ContainsSuggestion("gmtest.echo"), "F02: substring suggestion finds gmtest.echo");

            suggestionBuffer.Clear();
            gmManager.GetSuggestions("gmtest.e", suggestionBuffer);
            Assert(suggestionBuffer.Count > 0 && suggestionBuffer[0].Command == "gmtest.echo", "F03: prefix suggestion prioritizes gmtest.echo");

            suggestionBuffer.Clear();
            gmManager.GetSuggestions(string.Empty, suggestionBuffer);
            Assert(suggestionBuffer.Count == 0, "F04: empty input returns no suggestions");
        }

        private IEnumerator TestManualRegistration()
        {
            yield return PrepareLogContext();

            Action staticMethod = ManualStaticCommand;
            Action<int> instanceMethod = manualCommandHost.SetNumber;

            gmManager.UnregisterCommand(staticMethod);
            gmManager.UnregisterCommand(instanceMethod);

            int beforeCount = gmManager.CommandCount;

            bool staticRegistered = gmManager.RegisterCommand("gmtest.manual.static", "Manual static registration test.", staticMethod);
            Assert(staticRegistered, "G01: manual static method-group registration succeeds");

            bool instanceRegistered = gmManager.RegisterCommand<int>("gmtest.manual.instance", "Manual instance registration test.", manualCommandHost.SetNumber, "value");
            Assert(instanceRegistered, "G02: manual instance method-group registration succeeds");
            Assert(gmManager.CommandCount == beforeCount + 2, "G03: method-group registration increases command count");

            manualStaticInvoked = false;
            GmCommandExecutionResult result = gmManager.ExecuteCommand("gmtest.manual.static");
            Assert(result.Success, "G04: manual static method-group command executes successfully");
            Assert(manualStaticInvoked, "G05: manual static method-group side effect applied");

            manualInstanceValue = -1;
            result = gmManager.ExecuteCommand("gmtest.manual.instance [42]");
            Assert(result.Success, "G06: manual instance method-group command executes successfully");
            Assert(manualInstanceValue == 42, "G07: manual instance method-group parameter parsed and applied");
        }

        private IEnumerator TestUnregister()
        {
            yield return PrepareLogContext();

            Action staticMethod = ManualStaticCommand;
            Action<int> instanceMethod = manualCommandHost.SetNumber;

            gmManager.UnregisterCommand(staticMethod);
            gmManager.UnregisterCommand(instanceMethod);

            GmCommandExecutionResult result = gmManager.ExecuteCommand("gmtest.manual.static");
            Assert(!result.Success, "H01: method-group Unregister removes static command");

            result = gmManager.ExecuteCommand("gmtest.manual.instance [10]");
            Assert(!result.Success, "H02: method-group Unregister removes instance command");
        }

        private IEnumerator TestRegisterValidation()
        {
            yield return PrepareLogContext();

            gmManager.UnregisterCommand(((Action<int>)ValidationDuplicateCommand).Method);

            bool first = gmManager.RegisterCommand("gmtest.validation.duplicate", "First duplicate registration.", (Action<int>)ValidationDuplicateCommand, "value");
            bool second = gmManager.RegisterCommand("gmtest.validation.duplicate", "Second duplicate registration.", (Action<int>)ValidationDuplicateCommand, "value");

            Assert(first, "I01: first duplicate test registration succeeds");
            Assert(!second, "I02: duplicate same-arity registration is rejected");

            bool nonVoidRegistered = gmManager.RegisterCommand("gmtest.validation.return", "Non-void command must fail.", (Func<int>)ValidationNonVoidCommand);
            Assert(!nonVoidRegistered, "I03: non-void registration is rejected");

            bool unsupportedRegistered = gmManager.RegisterCommand("gmtest.validation.vector", "Unsupported parameter type must fail.", (Action<Vector3>)ValidationUnsupportedParameterCommand, "position");
            Assert(!unsupportedRegistered, "I04: unsupported parameter registration is rejected");

            gmManager.UnregisterCommand(((Action<int>)ValidationDuplicateCommand).Method);
            yield return null;
        }

        private IEnumerator PrepareLogContext()
        {
            if (logManager == null)
                yield break;

            logManager.Clear();
            logManager.SetFilter(LogLevel.All);
            logManager.SetCollapse(false);
            logManager.SetSearchTerm(string.Empty);

            yield return WaitForLogFrames();
        }

        private IEnumerator WaitForLogFrames()
        {
            yield return null;
            yield return null;
        }

        private bool HasVisibleLogContaining(string text)
        {
            if (logManager == null || string.IsNullOrEmpty(text))
                return false;

            for (int i = 0; i < logManager.VisibleCount; i++)
            {
                LogEntry entry = logManager.GetVisibleLog(i);
                if (entry != null && ContainsText(entry.LogString, text))
                    return true;
            }

            return false;
        }

        private bool ContainsSuggestion(string commandName)
        {
            for (int i = 0; i < suggestionBuffer.Count; i++)
            {
                if (string.Equals(suggestionBuffer[i].Command, commandName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool ContainsText(string source, string text)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(text))
                return false;

            return source.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildSuggestionText(List<GmCommandInfo> suggestions)
        {
            if (suggestions == null || suggestions.Count == 0)
                return "none";

            System.Text.StringBuilder builder = new System.Text.StringBuilder(128);
            for (int i = 0; i < suggestions.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(suggestions[i].Signature);
            }

            return builder.ToString();
        }

        private void Assert(bool condition, string message)
        {
            if (condition)
            {
                passCount++;
                Debug.Log($"<color=yellow>[GM PASS] {message}</color>");
            }
            else
            {
                failCount++;
                Debug.LogError($"<color=red>[GM FAIL] {message}</color>");
            }
        }

        private void PrintSummary()
        {
            string color = failCount == 0 ? "green" : "red";
            Debug.LogWarning($"<color={color}>======= GmManager Test: {passCount} passed, {failCount} failed =======</color>");
        }

        private static void ResetCommandState()
        {
            pingInvoked = false;
            echoedMessage = null;
            addResult = 0;
            toggleValue = false;
            modeValue = TestMode.Alpha;
            manualStaticInvoked = false;
            manualInstanceValue = -1;
        }

        [GmCommand("gmtest.ping", "Smoke test command without parameters.")]
        private static void GmPing()
        {
            pingInvoked = true;
            Debug.Log($"{LogPrefix} gmtest.ping invoked");
        }

        [GmCommand("gmtest.echo", "Echo a string parameter.", "message")]
        private static void GmEcho(string message)
        {
            echoedMessage = message;
            Debug.Log($"{LogPrefix} gmtest.echo => {message}");
        }

        [GmCommand("gmtest.add", "Add two integers and print the result.", "left")]
        private static void GmAdd(int left, int right)
        {
            addResult = left + right;
            Debug.Log($"{LogPrefix} gmtest.add => {left} + {right} = {addResult}");
        }

        [GmCommand("gmtest.toggle", "Print a bool parameter.", "enabled")]
        private static void GmToggle(bool enabled)
        {
            toggleValue = enabled;
            Debug.Log($"{LogPrefix} gmtest.toggle => enabled={enabled}");
        }

        [GmCommand("gmtest.mode", "Print an enum parameter.", "mode")]
        private static void GmMode(TestMode mode)
        {
            modeValue = mode;
            Debug.Log($"{LogPrefix} gmtest.mode => mode={mode}");
        }

        private static void ManualStaticCommand()
        {
            manualStaticInvoked = true;
            Debug.Log($"{LogPrefix} manual.static invoked");
        }

        private static void ValidationDuplicateCommand(int value)
        {
            Debug.Log($"{LogPrefix} validation.duplicate => {value}");
        }

        private static int ValidationNonVoidCommand()
        {
            return 1;
        }

        private static void ValidationUnsupportedParameterCommand(Vector3 position)
        {
            Debug.Log($"{LogPrefix} validation.vector => {position}");
        }
    }
}
