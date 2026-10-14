using Yooni.Native.Container;

namespace ReadyM.Api.Multiplayer.Interop;

internal unsafe delegate byte TryGetInternedIdDelegate(NativeString256* value, uint* id);

internal unsafe delegate byte TryResolveInternedIdDelegate(uint id, NativeString256* value);

internal delegate void ReportUnresolvedInternedIdDelegate(uint id);
