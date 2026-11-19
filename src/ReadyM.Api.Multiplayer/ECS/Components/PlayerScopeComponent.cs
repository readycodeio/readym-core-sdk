using System.ComponentModel;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// Holds the ID of the global player entity.
/// </summary>
/// <exclude />
[EditorBrowsable(EditorBrowsableState.Never)]
[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct PlayerScopeComponent : IIndexedComponent<PlayerId>, IKeyHeldOnce
{
    private byte _dirtyMask;
    private byte _apiMask;
    
    private PlayerId _playerId;
    
    public PlayerId GetIndexedValue() => _playerId;
}