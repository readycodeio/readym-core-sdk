using System.Runtime.InteropServices;
using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude />
/// <remarks>One mod seeder, as the mod host hands it to the server.</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct SeederDescriptor
{
    public SeederKind Kind;

    /// The seeder's owner: its mod's <c>Manifest.UniqueId</c>.
    public NativeString256 Owner;
}
