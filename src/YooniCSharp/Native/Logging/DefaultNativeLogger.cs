using System;
using Microsoft.Extensions.Logging;

namespace Yooni.Native.Logging;

public class DefaultNativeLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Console.WriteLine(formatter(state, exception));

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null!;
}
