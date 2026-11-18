using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude />
[StructLayout(LayoutKind.Sequential)]
public struct DimensionApiPointers
{
    public required IntPtr Create;
    public required IntPtr Destroy;
    public required IntPtr SetPlayerDimension;
    public required IntPtr Exists;
    public required IntPtr TryFind;
    public required IntPtr PlayerDimension;
    public required IntPtr CurrentDimension;
    public required IntPtr EntityDimension;
    public required IntPtr IsEntityOriginDimension;
}
