#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;

// An `init` accessor compiled against the netstandard2.0 build names the polyfill below in its signature, so
// this build has to resolve that name to the runtime's type or the accessor is a different method here.
[assembly: TypeForwardedTo(typeof(IsExternalInit))]

#else
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

public static class IsExternalInit;

#endif
