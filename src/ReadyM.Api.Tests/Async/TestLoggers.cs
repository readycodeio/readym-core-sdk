using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Tests.Async;

/// <summary>Fails the test if anything is logged at Error or above, so a happy-path test that unexpectedly logs
/// an error turns red (per the project test-logging rule).</summary>
internal sealed class FailOnErrorLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel >= LogLevel.Error)
            throw new InvalidOperationException(
                $"Unexpected {logLevel} log: {formatter(state, exception)}" +
                (exception is null ? string.Empty : $" | {exception}"));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>Records log entries so error-path tests can assert what was logged.</summary>
internal sealed class CapturingLogger : ILogger
{
    public readonly List<(LogLevel Level, string Message, Exception? Exception)> Entries = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
