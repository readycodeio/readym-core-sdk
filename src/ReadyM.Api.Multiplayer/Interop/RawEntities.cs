using System.Runtime.InteropServices;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Interop;

internal static class RawEntities
{
    public static RawEntity FromId(int entityId) => new Bits { Value = (uint)entityId }.Entity;

    public static RawEntity From(int entityId, short revision)
        => new Bits { Value = (uint)entityId | ((long)(ushort)revision << 32) }.Entity;

    // RawEntity is [StructLayout(Explicit)] over one long, Id at 0 and Revision at 4.
    // We do this not to make the constructor public in the Friflo fork.
    [StructLayout(LayoutKind.Explicit)]
    private struct Bits
    {
        [FieldOffset(0)]
        public long Value;

        [FieldOffset(0)]
        public RawEntity Entity;
    }
}