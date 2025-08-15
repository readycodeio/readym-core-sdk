using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public abstract class ShimDependencyTrackerImplBase<TCustomData> : IShimDependencyTrackerImpl
{
    public bool Supports(ShimRequestItem requestItem)
        => requestItem.CustomData is TCustomData customData && Supports(requestItem, customData);

    public bool Supports(ShimResponseItem responseItem)
        => responseItem.CustomData is TCustomData customData && Supports(responseItem, customData);

    public abstract bool Supports(ShimRequestItem requestItem, TCustomData customData);
    public abstract bool Supports(ShimResponseItem responseItem, TCustomData customData);

    public abstract bool CheckRequestHasResponse(ShimRequestItem requestItem, ShimResponseItem responseItem);
    public abstract bool CheckResponseShouldWait(ShimResponseItem responseItem, IRelayClientNetworkThreadContext context, IEnumerable<ShimRequestItem> requestItems);
}