using System.Runtime.InteropServices;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Tests.TestEvents;

[InteropType]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct NativeEvent : IAlwaysPropagates
{
    public IntPtr Actor { get; init; }
    public int IntValue { get; init; }
}