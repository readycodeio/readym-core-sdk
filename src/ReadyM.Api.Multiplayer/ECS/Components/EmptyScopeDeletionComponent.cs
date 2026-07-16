using ReadyM.Api.Attributes;
using ReadyM.Api.Multiplayer.Generators;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
internal partial struct EmptyScopeDeletionComponent
{
    private byte _dirtyMask;
    private byte _apiMask;

    private bool _doNotDeleteWhenAllPlayersLeave;
}
