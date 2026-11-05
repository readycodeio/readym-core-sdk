using ReadyM.Native.Container;

namespace ReadyM.Api.Multiplayer.Interning;

/// <summary>The strings the server or a client can write and read as ids.</summary>
internal interface IInternedStringIndex
{
    /// <summary>The id to write for a string, if it has one or, on the server, can be given one.</summary>
    bool TryGetIdForWrite(in NativeString256 value, out uint id);

    bool TryResolve(uint id, out NativeString256 value);

    /// <summary>Called by the reader for an id it could not resolve.</summary>
    void ReportUnresolved(uint id);
}
