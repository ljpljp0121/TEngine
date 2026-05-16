using System;

namespace PFDebugger
{
    /// <summary>
    /// 可被 GM 系统自动扫描并注册方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class GmCommandAttribute : Attribute
    {
        public string Command { get; }
        public string Description { get; }
        public string[] ParameterNames { get; }

        public GmCommandAttribute(string command)
            : this(command, string.Empty)
        {
        }

        public GmCommandAttribute(string command, string description, params string[] parameterNames)
        {
            Command = command?.Trim() ?? string.Empty;
            Description = description?.Trim() ?? string.Empty;
            ParameterNames = parameterNames ?? Array.Empty<string>();
        }
    }
}
