using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.ConflictResolution;

internal class NetworkTime(NetworkTimePointers pointers) : INetworkTime
{
    private readonly GetCurrentTimeDelegate _getCurrentTime = Marshal.GetDelegateForFunctionPointer<GetCurrentTimeDelegate>(pointers.GetCurrentTime);

    public uint GetCurrentTime()
        => _getCurrentTime();
}
