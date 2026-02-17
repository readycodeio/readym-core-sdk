using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using ReadyM.Api.Multiplayer.Client;

namespace ReadyM.Relay.Client.Host;

public class RelayClientService(IRelayClient relayClient, ILogger logger) : IAsyncDisposable
{
    private AsyncContextThread? _isolatedNoParallelismAsyncContextThread;
    private Task? _task;
    private CancellationTokenSource? _source;

    public IRelayClient RelayClient
        => relayClient;

    public bool IsRunning { get; private set; }

    public async ValueTask DisposeAsync()
    {
        if (IsRunning)
            await StopAsync();

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

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!IsRunning)
            return;

        logger.LogInformation("Stopping RelayClientService...");

        _source?.Cancel();

        if (_isolatedNoParallelismAsyncContextThread is not null)
            await _isolatedNoParallelismAsyncContextThread.JoinAsync();

        if (_task is not null)
            await _task;

        IsRunning = false;
        relayClient.Stop();

        logger.LogInformation("Stopped RelayClientService.");
    }
}