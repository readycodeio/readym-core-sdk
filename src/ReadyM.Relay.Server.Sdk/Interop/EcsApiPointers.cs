using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude/>
[StructLayout(LayoutKind.Sequential)]
public  struct EcsApiPointers
{
    public required IntPtr Query1WithIds;
    public required IntPtr Query2WithIds;
    public required IntPtr Query1;
    public required IntPtr Query2;
    public required IntPtr Query3;
    public required IntPtr Query4;
    public required IntPtr Query5;
    public required IntPtr Query6;
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
    public required IntPtr GetComponentPointer;
}