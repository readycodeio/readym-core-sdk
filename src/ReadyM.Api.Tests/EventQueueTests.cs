using Microsoft.Extensions.Logging;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.Tests;

public class EventQueueTests
{
    private struct Ping
    {
        public int Value;
    }

    private static EventQueue NewQueue()
        => new(LoggerFactory.Create(builder => builder.AddProvider(new FailOnErrorLoggerProvider())).CreateLogger("Tests"));

    [Fact]
    public void AOneArgumentHandlerRunsOncePerTypedInvoke()
    {
        var queue = NewQueue();
        var calls = 0;
        queue.RegisterHandler<Ping>(_ => calls++);

        queue.Invoke(new Ping { Value = 1 });

        Assert.Equal(1, calls);
    }

    [Fact]
    public void EveryHandlerRunsOnceGroupedByItsKeyInTheOrderTheKeyFirstAppeared()
    {
        var queue = NewQueue();
        var order = new List<string>();
        queue.RegisterHandler<Ping, string>((_, name) => order.Add(name), "first, with an argument");
        queue.RegisterHandler<Ping>(_ => order.Add("second, without"));
        queue.RegisterHandler<Ping, string>((_, name) => order.Add(name), "third, with an argument");
        queue.RegisterHandler<Ping>(_ => order.Add("fourth, without"));

        queue.Invoke(new Ping { Value = 1 });

        Assert.Equal(["first, with an argument", "third, with an argument", "second, without", "fourth, without"], order);
    }

    [Fact]
    public void TwoAndThreeArgumentHandlersRunOnceEach()
    {
        var queue = NewQueue();
        var calls = new List<string>();
        queue.RegisterHandler<Ping, string, int>((ev, name, n) => calls.Add($"{name} {n} {ev.Value}"), "two", 2);
        queue.RegisterHandler<Ping, string, int, bool>((ev, name, n, flag) => calls.Add($"{name} {n} {flag} {ev.Value}"), "three", 3, true);

        queue.Invoke(new Ping { Value = 7 });

        Assert.Equal(["two 2 7", "three 3 True 7"], calls);
    }
}
