using System;
using System.Collections.Generic;
using System.Globalization;

namespace PFDebugger
{
    /// <summary>
    /// GM 输入文本在拆分后的结果。
    /// </summary>
    internal readonly struct GmParsedInput
    {
        public string CommandName { get; }
        public IReadOnlyList<string> Arguments { get; }

        public GmParsedInput(string commandName, IReadOnlyList<string> arguments)
        {
            CommandName = commandName;
            Arguments = arguments;
        }
    }

    /// <summary>
    /// 负责把原始 GM 输入解析成命令名和 [] 参数列表。
    /// </summary>
    internal static class GmCommandParser
    {
        public static bool TryParse(string input, out GmParsedInput parsedInput, out string error)
        {
            parsedInput = default;
            error = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "GM command is empty.";
                return false;
            }

            input = input.Trim();

            int index = 0;
            while (index < input.Length && !char.IsWhiteSpace(input[index]))
                index++;

            string commandName = input.Substring(0, index);
            List<string> arguments = new List<string>(4);

            while (index < input.Length)
            {
                while (index < input.Length && char.IsWhiteSpace(input[index]))
                    index++;

                if (index >= input.Length)
                    break;

                if (input[index] != '[')
                {
                    error = "GM parameters must be wrapped with [].";
                    return false;
                }

                index++;
                int start = index;

                while (index < input.Length && input[index] != ']')
                    index++;

                if (index >= input.Length)
                {
                    error = "GM parameter is missing closing ']'.";
                    return false;
                }

                arguments.Add(input.Substring(start, index - start));
                index++;

                if (index < input.Length && !char.IsWhiteSpace(input[index]))
                {
                    error = "GM parameters must be separated by spaces.";
                    return false;
                }
            }

            parsedInput = new GmParsedInput(commandName, arguments);
            return true;
        }

        public static string ExtractCommandName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.TrimStart();
            int endIndex = 0;
            while (endIndex < input.Length && !char.IsWhiteSpace(input[endIndex]))
                endIndex++;

            return input.Substring(0, endIndex);
        }
    }

    /// <summary>
    /// 负责把字符串参数转换为 GM 系统支持的强类型值。
    /// </summary>
    internal static class GmValueParser
    {
        public static bool IsSupportedType(Type type)
        {
            if (type == typeof(string) ||
                type == typeof(int) ||
                type == typeof(float) ||
                type == typeof(bool))
            {
                return true;
            }

            return type != null && type.IsEnum;
        }

        public static string GetTypeDisplayName(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int))
                return "int";
            if (type == typeof(float))
                return "float";
            if (type == typeof(bool))
                return "bool";
            if (type != null && type.IsEnum)
                return type.Name;

            return type?.Name ?? "unknown";
        }

        public static bool TryParse(string rawValue, Type valueType, out object value, out string error)
        {
            value = null;
            error = null;

            if (valueType == typeof(string))
            {
                value = rawValue ?? string.Empty;
                return true;
            }

            if (valueType == typeof(int))
            {
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    value = intValue;
                    return true;
                }

                error = $"'{rawValue}' is not a valid int.";
                return false;
            }

            if (valueType == typeof(float))
            {
                if (float.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
                {
                    value = floatValue;
                    return true;
                }

                error = $"'{rawValue}' is not a valid float.";
                return false;
            }

            if (valueType == typeof(bool))
            {
                if (bool.TryParse(rawValue, out bool boolValue))
                {
                    value = boolValue;
                    return true;
                }

                if (string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rawValue, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (string.Equals(rawValue, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rawValue, "no", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                error = $"'{rawValue}' is not a valid bool.";
                return false;
            }

            if (valueType != null && valueType.IsEnum)
            {
                try
                {
                    value = Enum.Parse(valueType, rawValue, true);
                    return true;
                }
                catch
                {
                    error = $"'{rawValue}' is not a valid {valueType.Name}.";
                    return false;
                }
            }

            error = $"{valueType?.Name ?? "Unknown"} is not a supported GM parameter type.";
            return false;
        }
    }
}
