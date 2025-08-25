using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Relay.Client.Shim;

public class BlobClientShimParserImpl : ShimBuiltInMessageParserImplBase<ShimBlobDependencyData>
{
    public override bool SupportsRequest(ServerEventHeader header)
        => header.EventCode is RelayMessageCode.RequestUploadBlob or RelayMessageCode.RequestDownloadBlob;

    public override bool SupportsResponse(ServerEventHeader header)
        => header.EventCode is RelayMessageCode.UploadBlobAck or RelayMessageCode.DownloadBlobData;

    public override ShimBlobDependencyData GetBuiltInRequestCustomData(ServerEventHeader header, NetDataReader reader)
    {
        var requestId = reader.GetInt();
        return new ShimBlobDependencyData()
        {
            RequestId = requestId,
        };
    }

    public override ShimBlobDependencyData GetBuiltInResponseCustomData(ServerEventHeader header, NetDataReader reader)
    {
        var requestId = reader.GetInt();
        return new ShimBlobDependencyData()
        {
            RequestId = requestId,
        };
    }
}