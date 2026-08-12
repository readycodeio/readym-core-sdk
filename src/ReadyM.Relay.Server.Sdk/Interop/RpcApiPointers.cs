using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude/>
[StructLayout(LayoutKind.Sequential)]
public  struct RpcApiPointers
{
    public required IntPtr AddServerRpcMessageHandler;
    public required IntPtr RemoveServerRpcMessageHandler;
    public required IntPtr SendToOne;
}