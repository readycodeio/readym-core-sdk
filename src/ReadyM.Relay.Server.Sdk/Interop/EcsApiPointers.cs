using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct EcsApiPointers
{
    public IntPtr GetComponentIdByName;
    public IntPtr EmbedQuery1;
    public IntPtr EmbedQuery2;
}