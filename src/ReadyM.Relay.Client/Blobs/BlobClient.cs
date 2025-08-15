using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Client.Blobs;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client.Shim;

namespace ReadyM.Relay.Client.Blobs;

public class BlobClient : IBlobClient, IDisposable
{
    private readonly IRelayClient _relayClient;
    private readonly ILogger _logger;
    
    private int _requestCounter;
    private int GetNextRequestId() => ++_requestCounter;
    
    private readonly ConcurrentDictionary<int, TaskCompletionSource<BlobInfo?>> _blobDownloadTasks = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _blobUploadTasks = new();

    public event Action<IRelayClientNetworkThreadContext, int, bool>? OnUploadBlobAck;
    public event Action<IRelayClientNetworkThreadContext, int, BlobInfo?>? OnDownloadBlobData;

    public BlobClient(IRelayClient relayClient, ILogger logger)
    {
        _relayClient = relayClient;
        _logger = logger;

        _relayClient.AddBuiltInMessageHandler(RelayMessageCode.UploadBlobAck, OnUploadBlobAckHandler);
        _relayClient.AddBuiltInMessageHandler(RelayMessageCode.DownloadBlobData, OnDownloadBlobDataHandler);
    }

    public void Dispose()
    {
        _relayClient.RemoveBuiltInMessageHandler(RelayMessageCode.DownloadBlobData, OnDownloadBlobDataHandler);
        _relayClient.RemoveBuiltInMessageHandler(RelayMessageCode.UploadBlobAck, OnUploadBlobAckHandler);
    }

    private void OnUploadBlobAckHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var requestId = reader.GetInt();
        var success = reader.GetBool();

        _logger.LogInformation("File upload with request ID {RequestId} completed with success: {Success}", requestId, success);

        if (!_blobUploadTasks.TryRemove(requestId, out var uploadTask))
        {
            _logger.LogWarning("No task found for request ID {RequestId} when receiving upload ack", requestId);
            return;
        }

        if (uploadTask.Task.IsCanceled)
        {
            _logger.LogWarning("Upload task already cancelled, not setting result for request ID {RequestId}", requestId);
            return;
        }

        if (uploadTask.TrySetResult(success))
        {
            OnUploadBlobAck?.Invoke(context, requestId, success);
        }
        else
        {
            _logger.LogError("Failed to set result for file upload task with request ID {RequestId}", requestId);
        }
    }

    private void OnDownloadBlobDataHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var requestId = reader.GetInt();
        var succeeded = reader.GetBool();

        _logger.LogInformation("File download with request ID {RequestId} completed with success: {Succeeded}", requestId, succeeded);

        if (!_blobDownloadTasks.TryRemove(requestId, out var downloadTask))
        {
            _logger.LogError("No task found for request ID {RequestId}", requestId);
            return;
        }

        BlobInfo? result = null;

        if (succeeded)
        {
            var fileName = reader.GetString();
            var fileSize = reader.GetInt();

            var fileData = new byte[fileSize];
            reader.GetBytes(fileData, fileSize);

            _logger.LogInformation("Received file stream for {FileName} with request ID {RequestId}", fileName, requestId);
            result = new BlobInfo(fileName, fileData);
        }
        else
        {
            _logger.LogWarning("File download with request ID {RequestId} failed", requestId);
        }

        if (downloadTask.Task.IsCanceled)
        {
            _logger.LogWarning("Download task already cancelled, not setting result for request ID {RequestId}", requestId);
            return;
        }

        if (downloadTask.TrySetResult(result))
        {
            OnDownloadBlobData?.Invoke(context, requestId, result);
        }
        else
        {
            _logger.LogError("Failed to set result for file download task with request ID {RequestId}", requestId);
        }
    }

    public async Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default)
    {
        if (!_relayClient.RequestedConnect)
            throw new InvalidOperationException();

        ct.ThrowIfCancellationRequested();

        // add a default timeout of 15 seconds
        var nestedCt = CancellationTokenSource.CreateLinkedTokenSource(ct);
        nestedCt.CancelAfter(TimeSpan.FromSeconds(15));
        ct = nestedCt.Token;

        var tcs = new TaskCompletionSource<bool>();

        var requestId = GetNextRequestId();
        _blobUploadTasks[requestId] = tcs;

        var writer = new NetDataWriter();
        writer.Put((byte)RelayMessageCode.RequestUploadBlob);
        writer.Put(requestId);
        writer.Put(blob.Name);
        writer.Put(blob.Content.Length);
        writer.Put(blob.Content);
        _relayClient.SendRawMessage(writer, DeliveryMethod.ReliableOrdered);

        _logger.LogInformation("Uploading file: {FileName} with request ID {RequestId}", blob.Name, requestId);
        using (ct.Register(CancelCallback))
        {
            try
            {
                return await tcs.Task;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "File upload for {FileName} was cancelled with request ID {RequestId}", blob.Name, requestId);
                throw;
            }
            finally
            {
                _blobUploadTasks.TryRemove(requestId, out _);
            }
        }

        void CancelCallback()
        {
            _logger.LogWarning("Upload task for {FileName} with request ID {RequestId} was cancelled (TIMEOUT)", blob.Name, requestId);
            tcs.TrySetCanceled();
        }
    }

    public async Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        if (!_relayClient.RequestedConnect)
            throw new InvalidOperationException();

        ct.ThrowIfCancellationRequested();

        // add a default timeout of 10 seconds
        var nestedCt = CancellationTokenSource.CreateLinkedTokenSource(ct);
        nestedCt.CancelAfter(TimeSpan.FromSeconds(10));
        ct = nestedCt.Token;

        var tcs = new TaskCompletionSource<BlobInfo?>();

        var requestId = GetNextRequestId();
        _blobDownloadTasks[requestId] = tcs;

        var writer = new NetDataWriter();
        writer.Put((byte)RelayMessageCode.RequestDownloadBlob);
        writer.Put(requestId);
        writer.Put(name);
        _relayClient.SendRawMessage(writer, DeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Requesting file download: {FileName} with request ID {RequestId}", name, requestId);

        using (ct.Register(CancelCallback))
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                return await tcs.Task;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "File download for {FileName} was cancelled with request ID {RequestId}", name, requestId);
                throw;
            }
            finally
            {
                _blobDownloadTasks.TryRemove(requestId, out _);
            }
        }
        
        void CancelCallback()
        {
            _logger.LogWarning("Download task for {FileName} with request ID {RequestId} was cancelled (TIMEOUT)", name, requestId);
            tcs.TrySetCanceled();
        }
    }
}
