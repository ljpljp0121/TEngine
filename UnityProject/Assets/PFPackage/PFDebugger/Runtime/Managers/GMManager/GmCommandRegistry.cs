using System;
using System.Collections.Generic;
using System.Reflection;

namespace PFDebugger
{
    /// <summary>
    /// 管理 GM 命令的注册、查询、去重和建议检索。
    /// </summary>
    internal sealed class GmCommandRegistry
    {
        private static readonly Comparison<GmCommandInfo> suggestionSortComparison = CompareSuggestions;

        private readonly List<GmCommandInfo> commands = new List<GmCommandInfo>(32);
        private readonly Dictionary<string, List<GmCommandInfo>> commandMap = new Dictionary<string, List<GmCommandInfo>>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<GmCommandInfo> Commands => commands;
        public int Count => commands.Count;

        public void Clear()
        {
            commands.Clear();
            commandMap.Clear();
        }

        public bool Register(string command, string description, Delegate method, string[] parameterNames, out string error)
        {
            error = null;
            if (method == null)
            {
                error = "GM command delegate is null.";
                return false;
            }

            return Register(command, description, method.Method, method.Target, parameterNames, out error);
        }

        public bool Register(string command, string description, MethodInfo method, object instance, string[] parameterNames, out string error)
        {
            error = Validate(command, method, instance);
            if (error != null)
                return false;

            GmCommandInfo commandInfo = new GmCommandInfo(command.Trim(), description, method, instance, parameterNames);
            if (IsDuplicate(commandInfo, out error))
                return false;

            commands.Add(commandInfo);

            if (!commandMap.TryGetValue(commandInfo.Command, out List<GmCommandInfo> overloads))
            {
                overloads = new List<GmCommandInfo>(2);
                commandMap[commandInfo.Command] = overloads;
            }

            overloads.Add(commandInfo);
            return true;
        }

        public bool Unregister(MethodInfo method)
        {
            if (method == null)
                return false;

            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Method == method)
                    RemoveAt(i);
            }

            return true;
        }

        public bool TryGetCommands(string command, List<GmCommandInfo> results)
        {
            results.Clear();

            if (string.IsNullOrWhiteSpace(command))
                return false;

            if (!commandMap.TryGetValue(command, out List<GmCommandInfo> overloads))
                return false;

            results.AddRange(overloads);
            return results.Count > 0;
        }

        public void GetSuggestions(string input, List<GmCommandInfo> results)
        {
            results.Clear();

            string commandQuery = GmCommandParser.ExtractCommandName(input);
            if (string.IsNullOrEmpty(commandQuery))
                return;

            List<GmCommandInfo> prefixMatches = new List<GmCommandInfo>(8);
            List<GmCommandInfo> containsMatches = new List<GmCommandInfo>(8);

            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].Command.StartsWith(commandQuery, StringComparison.OrdinalIgnoreCase))
                    prefixMatches.Add(commands[i]);
            }

            for (int i = 0; i < commands.Count; i++)
            {
                GmCommandInfo command = commands[i];
                if (command.Command.StartsWith(commandQuery, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (command.Command.IndexOf(commandQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    containsMatches.Add(command);
            }

            prefixMatches.Sort(suggestionSortComparison);
            containsMatches.Sort(suggestionSortComparison);

            results.AddRange(prefixMatches);
            results.AddRange(containsMatches);
        }

        private static int CompareSuggestions(GmCommandInfo left, GmCommandInfo right)
        {
            int commandCompare = string.Compare(left?.Command, right?.Command, StringComparison.OrdinalIgnoreCase);
            if (commandCompare != 0)
                return commandCompare;

            return string.Compare(left?.Signature, right?.Signature, StringComparison.OrdinalIgnoreCase);
        }

        private string Validate(string command, MethodInfo method, object instance)
        {
            if (string.IsNullOrWhiteSpace(command))
                return "GM command name is empty.";

            if (method == null)
                return "GM command method is null.";

            if (!method.IsStatic && instance == null)
                return $"GM command '{command}' requires an instance.";

            if (method.ReturnType != typeof(void))
                return $"GM command '{command}' must return void.";

            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!GmValueParser.IsSupportedType(parameters[i].ParameterType))
                {
                    return $"GM command '{command}' uses unsupported parameter type '{parameters[i].ParameterType.Name}'.";
                }
            }

            return null;
        }

        private bool IsDuplicate(GmCommandInfo commandInfo, out string error)
        {
            error = null;

            if (!commandMap.TryGetValue(commandInfo.Command, out List<GmCommandInfo> overloads))
                return false;

            for (int i = 0; i < overloads.Count; i++)
            {
                if (overloads[i].Parameters.Count == commandInfo.Parameters.Count)
                {
                    error = $"GM command '{commandInfo.Command}' already has an overload with {commandInfo.Parameters.Count} parameter(s).";
                    return true;
                }
            }

            for (int i = 0; i < overloads.Count; i++)
            {
                if (overloads[i].HasSameSignatureAs(commandInfo))
                {
                    error = $"GM command '{commandInfo.Signature}' is already registered.";
                    return true;
                }
            }

            return false;
        }

        private void RemoveAt(int index)
        {
            GmCommandInfo command = commands[index];
            commands.RemoveAt(index);

            if (!commandMap.TryGetValue(command.Command, out List<GmCommandInfo> overloads))
                return;

            overloads.Remove(command);
            if (overloads.Count == 0)
                commandMap.Remove(command.Command);
        }
    }
}
