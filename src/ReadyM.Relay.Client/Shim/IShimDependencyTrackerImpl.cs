using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Shim;

namespace ReadyM.Relay.Client.Shim;

internal interface IShimDependencyTrackerImpl
{
    bool Supports(ShimRequestItem requestItem);
    bool Supports(ShimResponseItem responseItem);
    
    bool CheckRequestHasResponse(ShimRequestItem requestItem, ShimResponseItem responseItem);
    bool CheckResponseShouldWait(ShimResponseItem responseItem, IRelayClientNetworkThreadContext context, IEnumerable<ShimRequestItem> requestItems);
}