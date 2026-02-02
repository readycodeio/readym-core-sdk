using ReadyM.Api.Serialization;

namespace ReadyM.Relay.Client.Shim.Blobs;

[DeriveJsonSerializable]
public partial struct ShimBlobDependencyData(int requestId)
{
    public int RequestId = requestId;
}