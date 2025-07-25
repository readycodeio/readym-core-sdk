using System;
using ReadyM.Relay.Common;

namespace ReadyM.Relay.Client.Shim;

public interface IShimRecordableBlobClient : IBlobClient
{
    event Action<int, bool>? OnBlobAck;
    event Action<int, BlobInfo?>? OnBlobData;
}