using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.Shim;

namespace ReadyM.Relay.Client.Shim.ECS;

internal class ClientSynchronizerShimTrackerImpl : ShimDependencyTrackerImplBase<ShimEcsDependencyData>
{
    public override bool Supports(ShimRequestItem requestItem, ShimEcsDependencyData dependencyData)
        => requestItem is {
            Kind: ShimRequestKind.SentBuiltInMessage, 
            EventCode: 
            RelayMessageCode.EcsDelta or
            RelayMessageCode.EcsCreateEntity or
            RelayMessageCode.EcsDeleteEntity
        };

    public override bool Supports(ShimResponseItem responseItem, ShimEcsDependencyData dependencyData)
        => responseItem is
        {
            Kind: ShimResponseKind.AnyBuiltInMessage,
            ServerHeader.EventCode:
            RelayMessageCode.EcsSnapshot or
            RelayMessageCode.EcsDelta or
            RelayMessageCode.EcsCreateEntity or 
            RelayMessageCode.EcsDeleteEntity
        };

    public override bool CheckRequestHasResponse(ShimRequestItem requestItem, ShimResponseItem responseItem)
        // NOTE: Requests are not used to determine response replay
        => true;
    
    public override bool CheckResponseShouldWait(ShimResponseItem responseItem, IRelayClientNetworkThreadContext context, IEnumerable<ShimRequestItem> requestItems)
    {
        if (!context.IsConnected)
            return true;

        var responseData = responseItem.GetCustomData<ShimEcsDependencyData>();
        
        if (responseData.AreaId != null)
        {
            if (context.CurrentAreaId != responseData.AreaId)
                return true;
        }

        if (responseData.PlayerId != null)
        {
            if (context.AllPlayers.Contains(responseData.PlayerId.Value))
                return true;
        }

        return false;
    }
}