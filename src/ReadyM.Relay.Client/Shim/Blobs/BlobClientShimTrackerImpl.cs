using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public class BlobClientShimTrackerImpl : ShimDependencyTrackerImplBase<ShimBlobDependencyData>
{
    public override bool Supports(ShimRequestItem requestItem, ShimBlobDependencyData dependencyData)
        => requestItem is {
            Kind: ShimRequestKind.SentBuiltInMessage, 
            EventCode: 
            RelayMessageCode.RequestUploadBlob or
            RelayMessageCode.RequestDownloadBlob
        };

    public override bool Supports(ShimResponseItem responseItem, ShimBlobDependencyData dependencyData)
        => responseItem is
        {
            Kind: ShimResponseKind.AnyBuiltInMessage,
            EventCode:
            RelayMessageCode.UploadBlobAck or
            RelayMessageCode.DownloadBlobData
        };

    public override bool CheckRequestHasResponse(ShimRequestItem requestItem, ShimResponseItem responseItem)
    {
        if (requestItem.EventCode == RelayMessageCode.RequestUploadBlob && responseItem.EventCode == RelayMessageCode.UploadBlobAck) 
        {
            return requestItem.GetCustomData<ShimBlobDependencyData>().RequestId == responseItem.GetCustomData<ShimBlobDependencyData>().RequestId;
        }

        if (requestItem.EventCode == RelayMessageCode.RequestDownloadBlob && responseItem.EventCode == RelayMessageCode.DownloadBlobData) 
        {
            return requestItem.GetCustomData<ShimBlobDependencyData>().RequestId == responseItem.GetCustomData<ShimBlobDependencyData>().RequestId;
        }

        return false;
    }

    public override bool CheckResponseShouldWait(ShimResponseItem responseItem, IRelayClientNetworkThreadContext context, IEnumerable<ShimRequestItem> requestItems)
    {
        foreach (var requestItem in requestItems)
        {
            if (responseItem.CustomData is ShimBlobDependencyData responseData &&
                requestItem is { Kind: ShimRequestKind.SentBuiltInMessage, CustomData: ShimBlobDependencyData requestData } &&
                ((responseItem.EventCode is RelayMessageCode.UploadBlobAck && requestItem.EventCode is RelayMessageCode.RequestUploadBlob) ||
                 (responseItem.EventCode is RelayMessageCode.DownloadBlobData && requestItem.EventCode is RelayMessageCode.RequestDownloadBlob)) &&
                responseData.RequestId == requestData.RequestId)
            {
                return false;
            }
        }

        return true;
    }
}