using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.Interop;

namespace ReadyM.Relay.Server.Sdk.ConflictResolution;

public class NetworkTime(NetworkTimePointers pointers) : INetworkTime
{
    private readonly GetCurrentTimeDelegate _getCurrentTime = Marshal.GetDelegateForFunctionPointer<GetCurrentTimeDelegate>(pointers.GetCurrentTime);
    private readonly AdvanceTimeDelegate _advanceTime = Marshal.GetDelegateForFunctionPointer<AdvanceTimeDelegate>(pointers.AdvanceTime);

    public uint GetCurrentTime()
        => _getCurrentTime();

    public void AdvanceTime()
        => _advanceTime();
}
