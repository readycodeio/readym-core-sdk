using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude/>
[StructLayout(LayoutKind.Sequential)]
public struct RpcApiPointers
{
    public required IntPtr AddServerRpcMessageHandler;
    public required IntPtr RemoveServerRpcMessageHandler;
    public required IntPtr SendToOne;

    /// <summary>Tells the relay what a mod's RPC or networked event code is called, for its metrics.</summary>
    public required IntPtr NameMessageCode;
}