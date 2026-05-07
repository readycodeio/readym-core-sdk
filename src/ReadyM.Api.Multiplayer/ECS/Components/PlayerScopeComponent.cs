using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
internal partial struct PlayerScopeComponent : IIndexedComponent<PlayerId>
{
    private byte _dirtyMask;
    
    private PlayerId _playerId;
    
    public PlayerId GetIndexedValue()
        => PlayerId;
}