using System;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Client.Blobs;
using ReadyM.Relay.Client.Blobs;

namespace ReadyM.Relay.Client.Shim;

public interface IShimRecordableBlobClient : IBlobClient
{
    event Action<IRelayClientNetworkThreadContext, int, bool>? OnUploadBlobAck;
    event Action<IRelayClientNetworkThreadContext, int, BlobInfo?>? OnDownloadBlobData;
}
