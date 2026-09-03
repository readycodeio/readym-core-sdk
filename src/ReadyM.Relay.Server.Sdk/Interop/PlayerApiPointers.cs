using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude />
[StructLayout(LayoutKind.Sequential)]
public struct PlayerApiPointers
{
    public required IntPtr AddPlayerEventHandler;
    public required IntPtr RemovePlayerEventHandler;
    public required IntPtr KickPlayer;
    public required IntPtr GetReadyMId;
}
