using System;
using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude />
[StructLayout(LayoutKind.Sequential)]
public struct InternedStringsPointers
{
    public required IntPtr TryGetId;
    public required IntPtr TryResolve;
    public required IntPtr ReportUnresolved;
}
