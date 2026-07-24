using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct PlayerScopeComponent : IIndexedComponent<PlayerId>
{
    private byte _dirtyMask;
    private byte _apiMask;
    
    private PlayerId _playerId;
    
    public PlayerId GetIndexedValue() => _playerId;
}