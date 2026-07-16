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

    /// <summary>
    /// When this is set to true, the scope and entitites inside of it will not be deleted even when all players leave it.
    /// Cells and their parent areas do not inherit that value from one another.
    /// This means that a cell with this flag set will still be deleted if all players leave its parent area if that area doesn't have this flag set.
    /// Similarly, a cell which doesn't have this flag set will be deleted when all players deactivate it (directly or by leaving its parent area),
    /// even if its parent area has thi flag set.
    /// Setting this flag to false on a scope that has no players in it won't cause the scope to be deleted until one or more players join it
    /// and then all players leave it again.
    /// </summary>
    private bool _doNotDeleteWhenAllPlayersLeave;
}
