namespace PFGAS.Runtime
{
    public readonly struct GASFailure
    {
        public GASFailure(string reason, string message = null)
        {
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static GASFailure None => default;

        public string Reason { get; }

        public string Message { get; }

        public bool IsValid => !string.IsNullOrEmpty(Reason) || !string.IsNullOrEmpty(Message);

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Message))
            {
                return Reason ?? string.Empty;
            }

            if (string.IsNullOrEmpty(Reason))
            {
                return Message;
            }

            return Reason + ": " + Message;
        }
    }

    public readonly struct GASResult
    {
        private GASResult(bool succeeded, GASFailure failure)
        {
            Succeeded = succeeded;
            Failure = failure;
        }

        public bool Succeeded { get; }

        public bool Failed => !Succeeded;

        public GASFailure Failure { get; }

        public static GASResult Success()
        {
            return new GASResult(true, GASFailure.None);
        }

        public static GASResult Fail(string reason, string message = null)
        {
            return new GASResult(false, new GASFailure(reason, message));
        }

        public static GASResult Fail(GASFailure failure)
        {
            return new GASResult(false, failure);
        }
    }

    public readonly struct GASResult<T>
    {
        private GASResult(bool succeeded, T value, GASFailure failure)
        {
            Succeeded = succeeded;
            Value = value;
            Failure = failure;
        }

        public bool Succeeded { get; }

        public bool Failed => !Succeeded;

        public T Value { get; }

        public GASFailure Failure { get; }

        public static GASResult<T> Success(T value)
        {
            return new GASResult<T>(true, value, GASFailure.None);
        }

        public static GASResult<T> Fail(string reason, string message = null)
        {
            return new GASResult<T>(false, default, new GASFailure(reason, message));
        }

        public static GASResult<T> Fail(GASFailure failure)
        {
            return new GASResult<T>(false, default, failure);
        }
    }
}
