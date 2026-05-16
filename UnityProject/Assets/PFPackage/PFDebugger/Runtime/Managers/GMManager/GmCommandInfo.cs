using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PFDebugger
{
    /// <summary>
    /// GM 命令的单个参数信息。
    /// </summary>
    public sealed class GmCommandParameterInfo
    {
        public string Name { get; }
        public Type ParameterType { get; }
        public string TypeDisplayName { get; }

        internal GmCommandParameterInfo(string name, Type parameterType)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "arg" : name.Trim();
            ParameterType = parameterType;
            TypeDisplayName = GmValueParser.GetTypeDisplayName(parameterType);
        }
    }

    /// <summary>
    /// 已注册 GM 命令的所有数据。
    /// </summary>
    public sealed class GmCommandInfo
    {
        private readonly GmCommandParameterInfo[] parameters;

        public string Command { get; }
        public string Description { get; }
        public MethodInfo Method { get; }
        public object Instance { get; }
        public IReadOnlyList<GmCommandParameterInfo> Parameters => parameters;
        public bool IsStatic => Method.IsStatic;
        public string Signature { get; }

        internal GmCommandInfo(string command, string description, MethodInfo method, object instance, string[] parameterNames)
        {
            Command = command;
            Description = description ?? string.Empty;
            Method = method;
            Instance = instance;

            var reflectedParameters = method.GetParameters();
            parameters = new GmCommandParameterInfo[reflectedParameters.Length];
            for (int i = 0; i < reflectedParameters.Length; i++)
            {
                string parameterName = parameterNames != null && i < parameterNames.Length && !string.IsNullOrWhiteSpace(parameterNames[i])
                    ? parameterNames[i]
                    : reflectedParameters[i].Name;
                parameters[i] = new GmCommandParameterInfo(parameterName, reflectedParameters[i].ParameterType);
            }

            Signature = BuildSignature(command, parameters);
        }

        internal void Invoke(object[] args)
        {
            Method.Invoke(Instance, args);
        }

        internal bool HasSameSignatureAs(GmCommandInfo other)
        {
            if (other == null)
                return false;

            if (!string.Equals(Command, other.Command, StringComparison.OrdinalIgnoreCase))
                return false;

            if (parameters.Length != other.parameters.Length)
                return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != other.parameters[i].ParameterType)
                    return false;
            }

            return true;
        }

        private static string BuildSignature(string command, IReadOnlyList<GmCommandParameterInfo> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return command;

            StringBuilder builder = new StringBuilder(command.Length + parameters.Count * 12);
            builder.Append(command);

            for (int i = 0; i < parameters.Count; i++)
            {
                builder.Append(" [")
                    .Append(parameters[i].Name)
                    .Append(':')
                    .Append(parameters[i].TypeDisplayName)
                    .Append(']');
            }

            return builder.ToString();
        }
    }
}
