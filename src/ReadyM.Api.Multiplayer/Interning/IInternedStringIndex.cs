using Yooni.Native.Container;

namespace ReadyM.Api.Multiplayer.Interning;

/// The strings the server or a client can write and read as ids.
public interface IInternedStringIndex
{
    /// The id to write for a string, if it has one or, on the server, can be given one.
    bool TryGetIdForWrite(in NativeString256 value, out uint id);

    bool TryResolve(uint id, out NativeString256 value);

    /// Called by the reader for an id it could not resolve.
    void ReportUnresolved(uint id);
}
