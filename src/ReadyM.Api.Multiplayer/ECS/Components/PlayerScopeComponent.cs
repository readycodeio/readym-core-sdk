using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
internal partial struct PlayerScopeComponent : IIndexedComponent<PlayerId>
{
    private PlayerId _playerId;
    
    public PlayerId GetIndexedValue()
        => PlayerId;
}