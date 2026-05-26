using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Multiplayer.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct HostPointers
{
    public IntPtr GetComponentIdByName;
    public IntPtr EmbedQuery1;
    public IntPtr EmbedQuery2;
}