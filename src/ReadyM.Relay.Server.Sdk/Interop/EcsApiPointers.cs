using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct EcsApiPointers
{
    // Legacy - kept for existing GetNetworkComponent<T>() path (still string-based)
    public required IntPtr GetComponentIdByName;

    // Plugin component registration - call during plugin Init() before any entity creation
    public required IntPtr RegisterPluginComponent;

    // Unified queries - accept int component IDs for both AOT and plugin components
    public required IntPtr Query1;
    public required IntPtr Query2;
    public required IntPtr Query3;
    public required IntPtr Query4;
    public required IntPtr Query5;
    public required IntPtr Query6;
}