using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

[StructLayout(LayoutKind.Sequential)]
public  struct EcsApiPointers
{
    public required IntPtr Query1;
    public required IntPtr Query2;
    public required IntPtr Query3;
    public required IntPtr Query4;
    public required IntPtr Query5;
    public required IntPtr Query6;
    public required IntPtr CreateNetworkedEntity;
    public required IntPtr GetComponentPointer;
}