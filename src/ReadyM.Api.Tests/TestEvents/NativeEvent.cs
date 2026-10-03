using System.Runtime.InteropServices;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.Tests.TestEvents;

[InteropType, DeriveIGameEvent, AlwaysPropagates]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct NativeEvent
{
    public IntPtr Actor { get; init; }
    public int IntValue { get; init; }
}