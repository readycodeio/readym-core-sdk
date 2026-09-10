using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude/>
[StructLayout(LayoutKind.Sequential)]
public  struct EcsApiPointers
{
    /// <summary>Arity-agnostic query. Supersedes Query1..Query6.</summary>
    public required IntPtr Query;

    public required IntPtr CreateNetworkedEntity;
    public required IntPtr CreateNetworkedPlayerEntity;
    public required IntPtr CreateNetworkedAreaEntity;
    public required IntPtr CreateNetworkedCellEntity;
    public required IntPtr CreateLocalEntity;
    public required IntPtr DeleteNetworkedEntity;
    public required IntPtr DeleteEntityTree;
    public required IntPtr SetParent;
    public required IntPtr GetParent;
    public required IntPtr GetChildren;
    public required IntPtr GetComponentSlot;
}