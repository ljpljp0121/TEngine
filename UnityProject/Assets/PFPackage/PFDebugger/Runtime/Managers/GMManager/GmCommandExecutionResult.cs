namespace PFDebugger
{
    /// <summary>
    /// GM 命令执行后的结果。
    /// </summary>
    public readonly struct GmCommandExecutionResult
    {
        public bool Success { get; }
        public string Message { get; }
        public GmCommandInfo Command { get; }

        public GmCommandExecutionResult(bool success, string message, GmCommandInfo command)
        {
            Success = success;
            Message = message;
            Command = command;
        }

        public static GmCommandExecutionResult Failed(string message)
        {
            return new GmCommandExecutionResult(false, message, null);
        }

        public static GmCommandExecutionResult Succeeded(GmCommandInfo command, string message = null)
        {
            return new GmCommandExecutionResult(true, message, command);
        }
    }
}
