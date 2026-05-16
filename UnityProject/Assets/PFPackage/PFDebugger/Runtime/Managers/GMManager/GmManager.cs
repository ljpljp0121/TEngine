using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PFDebugger
{
    [SubManager(1)]
    public class GmManager : SubManagerBase
    {
        private static readonly string[] ignoredAssemblyPrefixes =
        {
            "Unity",
            "System",
            "Mono.",
            "mscorlib",
            "netstandard",
            "TextMeshPro",
            "nunit.",
            "Microsoft.",
            "Microsoft.GeneratedCode",
            "I18N",
            "Boo.",
            "UnityScript.",
            "ICSharpCode.",
            "ExCSS.Unity",
#if UNITY_EDITOR
            "Assembly-CSharp-Editor",
            "Assembly-UnityScript-Editor",
            "nunit.",
            "SyntaxTree.",
            "AssetStoreTools",
#endif
        };

        private readonly GmCommandRegistry registry = new GmCommandRegistry();
        private readonly List<GmCommandInfo> tempCommands = new List<GmCommandInfo>(8);

        /// <summary> 当前命令数量 </summary>
        public int CommandCount => registry.Count;
        public IReadOnlyList<GmCommandInfo> Commands => registry.Commands;

        public override void Init()
        {
            RegisterBuiltInCommands();
            RegisterAttributedCommands();
        }

        public override void DeInit()
        {
            registry.Clear();
            tempCommands.Clear();
        }

        private GmCommandExecutionResult TryExecuteInternal(string input)
        {
            if (!GmCommandParser.TryParse(input, out GmParsedInput parsedInput, out string parseError))
                return GmCommandExecutionResult.Failed(parseError);

            if (!registry.TryGetCommands(parsedInput.CommandName, tempCommands))
                return GmCommandExecutionResult.Failed(BuildUnknownCommandMessage(parsedInput.CommandName));

            GmCommandInfo command = FindCommandByArgumentCount(tempCommands, parsedInput.Arguments.Count);
            if (command == null)
                return GmCommandExecutionResult.Failed(BuildArgumentCountError(parsedInput.CommandName, tempCommands));

            object[] invokeArguments = new object[command.Parameters.Count];
            for (int i = 0; i < invokeArguments.Length; i++)
            {
                if (!GmValueParser.TryParse(parsedInput.Arguments[i], command.Parameters[i].ParameterType,
                        out object parsedValue, out string valueParseError))
                {
                    return GmCommandExecutionResult.Failed(
                        $"Command '{command.Command}' argument {i + 1} parse failed: {valueParseError} Usage: {command.Signature}");
                }

                invokeArguments[i] = parsedValue;
            }

            try
            {
                command.Invoke(invokeArguments);
                return GmCommandExecutionResult.Succeeded(command);
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                return GmCommandExecutionResult.Failed(
                    $"Command '{command.Command}' threw {inner.GetType().Name}: {inner.Message}");
            }
            catch (Exception ex)
            {
                return GmCommandExecutionResult.Failed(
                    $"Command '{command.Command}' failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void RegisterBuiltInCommands()
        {
            RegisterCommand("help", "List all GM commands.", (Action)LogHelpText);
            RegisterCommand("help", "Show a GM command signature.", (Action<string>)LogHelpTextForCommand, "command");
        }

        private void RegisterAttributedCommands()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Assembly assembly = assemblies[assemblyIndex];
                if (ShouldIgnoreAssembly(assembly))
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    MethodInfo[] methods = types[typeIndex]
                        .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                    {
                        object[] attributes =
                            methods[methodIndex].GetCustomAttributes(typeof(GmCommandAttribute), false);
                        for (int attributeIndex = 0; attributeIndex < attributes.Length; attributeIndex++)
                        {
                            GmCommandAttribute attribute = (GmCommandAttribute)attributes[attributeIndex];
                            RegisterCommand(attribute.Command, attribute.Description, methods[methodIndex], null,
                                attribute.ParameterNames);
                        }
                    }
                }
            }
        }

        private static bool ShouldIgnoreAssembly(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;
            for (int i = 0; i < ignoredAssemblyPrefixes.Length; i++)
            {
                if (assemblyName.StartsWith(ignoredAssemblyPrefixes[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static GmCommandInfo FindCommandByArgumentCount(List<GmCommandInfo> commands, int argumentCount)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].Parameters.Count == argumentCount)
                    return commands[i];
            }

            return null;
        }

        private void LogHelpText()
        {
            if (registry.Count == 0)
            {
                Debug.Log("[PFDebugger][GM] No GM commands registered.");
                return;
            }

            StringBuilder builder = new StringBuilder(256);
            builder.Append("GM commands:");
            for (int i = 0; i < registry.Commands.Count; i++)
            {
                GmCommandInfo command = registry.Commands[i];
                builder.Append("\n - ").Append(command.Signature);
                if (!string.IsNullOrEmpty(command.Description))
                    builder.Append(" : ").Append(command.Description);
            }

            Debug.Log($"[PFDebugger][GM] {builder}");
        }

        private void LogHelpTextForCommand(string commandName)
        {
            if (!registry.TryGetCommands(commandName, tempCommands))
            {
                Debug.LogError($"[PFDebugger][GM] {BuildUnknownCommandMessage(commandName)}");
                return;
            }

            StringBuilder builder = new StringBuilder(128);
            builder.Append("GM help for '").Append(commandName).Append("':");
            for (int i = 0; i < tempCommands.Count; i++)
            {
                builder.Append("\n - ").Append(tempCommands[i].Signature);
                if (!string.IsNullOrEmpty(tempCommands[i].Description))
                    builder.Append(" : ").Append(tempCommands[i].Description);
            }

            Debug.Log($"[PFDebugger][GM] {builder}");
        }

        private string BuildUnknownCommandMessage(string commandName)
        {
            List<GmCommandInfo> suggestions = new List<GmCommandInfo>(4);
            registry.GetSuggestions(commandName, suggestions);
            if (suggestions.Count == 0)
                return $"Unknown GM command '{commandName}'.";

            StringBuilder builder = new StringBuilder(128);
            builder.Append("Unknown GM command '").Append(commandName).Append("'. Did you mean:");
            for (int i = 0; i < suggestions.Count && i < 5; i++)
                builder.Append("\n - ").Append(suggestions[i].Signature);

            return builder.ToString();
        }

        private static string BuildArgumentCountError(string commandName, List<GmCommandInfo> overloads)
        {
            if (overloads.Count == 0)
                return $"Unknown GM command '{commandName}'.";

            StringBuilder builder = new StringBuilder(128);
            builder.Append("Argument count mismatch for '").Append(commandName).Append("'. Valid usage:");
            for (int i = 0; i < overloads.Count; i++)
                builder.Append("\n - ").Append(overloads[i].Signature);

            return builder.ToString();
        }

        #region 对外接口

        public void GetSuggestions(string input, List<GmCommandInfo> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            registry.GetSuggestions(input, results);
        }

        public bool Execute(string input)
        {
            return ExecuteCommand(input).Success;
        }

        public GmCommandExecutionResult ExecuteCommand(string input)
        {
            GmCommandExecutionResult result = TryExecuteInternal(input);

            if (!result.Success)
                Debug.LogError($"[PFDebugger][GM] {result.Message}");

            return result;
        }

        public bool RegisterCommand(string command, string description, Action method)
        {
            return RegisterCommand(command, description, (Delegate)method);
        }

        public bool RegisterCommand<T1>(string command, string description, Action<T1> method,
            string parameterName1 = null)
        {
            return RegisterCommand(command, description, (Delegate)method, parameterName1);
        }

        public bool RegisterCommand<T1, T2>(string command, string description, Action<T1, T2> method,
            string parameterName1 = null, string parameterName2 = null)
        {
            return RegisterCommand(command, description, (Delegate)method, parameterName1, parameterName2);
        }

        public bool RegisterCommand<T1, T2, T3>(string command, string description, Action<T1, T2, T3> method,
            string parameterName1 = null, string parameterName2 = null, string parameterName3 = null)
        {
            return RegisterCommand(command, description, (Delegate)method, parameterName1, parameterName2,
                parameterName3);
        }

        public bool RegisterCommand<T1, T2, T3, T4>(string command, string description, Action<T1, T2, T3, T4> method,
            string parameterName1 = null, string parameterName2 = null, string parameterName3 = null,
            string parameterName4 = null)
        {
            return RegisterCommand(command, description, (Delegate)method, parameterName1, parameterName2,
                parameterName3, parameterName4);
        }

        public bool RegisterCommand(string command, string description, Delegate method, params string[] parameterNames)
        {
            bool success = registry.Register(command, description, method, parameterNames, out string error);
            if (!success)
                Debug.LogError($"[PFDebugger][GM] {error}");

            return success;
        }

        public bool RegisterCommand(string command, string description, MethodInfo method, object instance = null,
            params string[] parameterNames)
        {
            bool success = registry.Register(command, description, method, instance, parameterNames, out string error);
            if (!success)
                Debug.LogError($"[PFDebugger][GM] {error}");

            return success;
        }

        public void UnregisterCommand(Action method)
        {
            UnregisterCommand(method?.Method);
        }

        public void UnregisterCommand<T1>(Action<T1> method)
        {
            UnregisterCommand(method?.Method);
        }

        public void UnregisterCommand<T1, T2>(Action<T1, T2> method)
        {
            UnregisterCommand(method?.Method);
        }

        public void UnregisterCommand<T1, T2, T3>(Action<T1, T2, T3> method)
        {
            UnregisterCommand(method?.Method);
        }

        public void UnregisterCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method)
        {
            UnregisterCommand(method?.Method);
        }

        public void UnregisterCommand(MethodInfo method)
        {
            registry.Unregister(method);
        }

        #endregion
    }
}