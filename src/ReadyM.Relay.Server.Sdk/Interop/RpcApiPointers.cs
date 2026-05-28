using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct RpcApiPointers
{
    public IntPtr AddServerRpcMessageHandler;
    public IntPtr RemoveServerRpcMessageHandler;
}