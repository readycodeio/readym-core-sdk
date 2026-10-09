using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using ReadyM.Api.Multiplayer.Client;

namespace ReadyM.Relay.Client.Host;

internal sealed class RelayClientService(IRelayClient relayClient, ILogger logger) : IDisposable
{
    private AsyncContextThread? _isolatedNoParallelismAsyncContextThread;
    private Task? _task;
    private CancellationTokenSource? _source;

    public bool IsRunning { get; private set; }

    public void Dispose()
    {
        if (IsRunning)
            Stop();

        _isolatedNoParallelismAsyncContextThread?.Dispose();
        _task?.Dispose();
        _source?.Dispose();
    }

    public void Start()
    {
        if (IsRunning)
            return;
        IsRunning = true;

        logger.LogInformation("Starting RelayClientService...");

        _source = new CancellationTokenSource();
        var stoppingToken = _source.Token;

        var startedEvent = new ManualResetEventSlim();

        _isolatedNoParallelismAsyncContextThread = new AsyncContextThread();

        _task = _isolatedNoParallelismAsyncContextThread.Factory.Run(async () =>
        {
            try
            {
                relayClient.Start();
            }
            finally
            {
                startedEvent.Set();
            }

            await relayClient.RunAsync(stoppingToken);
        });

        startedEvent.Wait(stoppingToken);

        logger.LogInformation("Started RelayClientService.");
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        logger.LogInformation("Stopping RelayClientService...");

        // Stopped before the loop is cancelled, so its own disconnect is handled on its network thread.
        relayClient.Stop();

        _source?.Cancel();

        // The task first: its continuations run on the context thread, which Join shuts down.
        _task?.GetAwaiter().GetResult();
        _isolatedNoParallelismAsyncContextThread?.Join();

        IsRunning = false;

        logger.LogInformation("Stopped RelayClientService.");
    }
}