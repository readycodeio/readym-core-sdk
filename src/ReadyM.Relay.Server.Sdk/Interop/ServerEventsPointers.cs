using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude />
[StructLayout(LayoutKind.Sequential)]
public struct ServerEventsPointers
{
    public required IntPtr Subscribe;
    public required IntPtr Unsubscribe;
}
