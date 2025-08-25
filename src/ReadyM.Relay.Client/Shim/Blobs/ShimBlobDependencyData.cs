using ReadyM.Api.Serialization;

namespace ReadyM.Relay.Client.Shim;

[DeriveJsonSerializable]
public partial struct ShimBlobDependencyData(int requestId)
{
    public int RequestId = requestId;
}