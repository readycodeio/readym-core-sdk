using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using ReadyM.Api.Multiplayer.Client;

namespace ReadyM.Relay.Client.Host;

internal class RelayClientService(IRelayClient relayClient, ILogger logger) : IDisposable
{
    private AsyncContextThread? _isolatedNoParallelismAsyncContextThread;
    private Task? _task;
    private CancellationTokenSource? _source;

    public IRelayClient RelayClient
        => relayClient;

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

        _source?.Cancel();

        _isolatedNoParallelismAsyncContextThread?.Join();
        _task?.GetAwaiter().GetResult();

        IsRunning = false;
        relayClient.Stop();

        logger.LogInformation("Stopped RelayClientService.");
    }
}