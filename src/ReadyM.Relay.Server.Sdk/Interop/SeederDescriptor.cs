using System.Runtime.InteropServices;
using ReadyM.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude />
/// <remarks>One mod seeder, as the mod host hands it to the server.</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct SeederDescriptor
{
    public SeederKind Kind;

    /// <summary>The seeder's owner: its mod's <c>Manifest.UniqueId</c>.</summary>
    public NativeString256 Owner;
}
