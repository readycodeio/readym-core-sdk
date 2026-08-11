using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct PlayerApiPointers
{
    public required IntPtr AddPlayerEventHandler;
    public required IntPtr RemovePlayerEventHandler;
    public required IntPtr KickPlayer;
}
