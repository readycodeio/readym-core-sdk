using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Interning;
using ReadyM.Api.Multiplayer.Interop;
using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// The relay's index, used by the mod-generated code.
internal sealed unsafe class RelayInternedStringIndex(InternedStringsPointers pointers) : IInternedStringIndex
{
    private readonly TryGetInternedIdDelegate _tryGetId
        = Marshal.GetDelegateForFunctionPointer<TryGetInternedIdDelegate>(pointers.TryGetId);

    private readonly TryResolveInternedIdDelegate _tryResolve
        = Marshal.GetDelegateForFunctionPointer<TryResolveInternedIdDelegate>(pointers.TryResolve);

    private readonly ReportUnresolvedInternedIdDelegate _reportUnresolved
        = Marshal.GetDelegateForFunctionPointer<ReportUnresolvedInternedIdDelegate>(pointers.ReportUnresolved);

    public bool TryGetIdForWrite(in NativeString256 value, out uint id)
    {
        fixed (NativeString256* pointer = &value)
        {
            uint found;
            var known = _tryGetId(pointer, &found) != 0;
            id = found;
            return known;
        }
    }

    public bool TryResolve(uint id, out NativeString256 value)
    {
        NativeString256 found;
        var known = _tryResolve(id, &found) != 0;
        value = found;
        return known;
    }

    public void ReportUnresolved(uint id)
        => _reportUnresolved(id);
}
