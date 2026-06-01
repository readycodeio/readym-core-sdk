using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct EcsApiPointers
{
    public required IntPtr GetComponentIdByName;
    public required IntPtr EmbedQuery1;
    public required IntPtr EmbedQuery2;
}