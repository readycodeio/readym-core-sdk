using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Shim;

namespace ReadyM.Relay.Client.Shim;

internal class ShimReplayDependencyTracker(IEnumerable<IShimDependencyTrackerImpl> impls)
{
    private readonly List<IShimDependencyTrackerImpl> _impls = [..impls];

    public bool CheckRequestCanDelete(ShimRequestItem requestItem, ShimResponseItem responseItem)
    {
        foreach (var impl in _impls)
        {
            if (!impl.Supports(requestItem))
                continue;
            if (!impl.Supports(responseItem))
                continue;
            
            if (impl.CheckRequestHasResponse(requestItem, responseItem))
                return true;
        }
        
        return false;
    }

    public bool CheckResponseShouldWait(ShimResponseItem responseItem, IRelayClientNetworkThreadContext context, IEnumerable<ShimRequestItem> requestItems)
    {
        foreach (var impl in _impls)
        {
            if (!impl.Supports(responseItem))
                continue;
            
            // ReSharper disable once PossibleMultipleEnumeration
            if (impl.CheckResponseShouldWait(responseItem, context, requestItems))
                return true;
        }
        
        return false;
    }
}