using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Multiplayer.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct EcsApiPointers
{
    public IntPtr GetComponentIdByName;
    public IntPtr EmbedQuery1;
    public IntPtr EmbedQuery2;
}