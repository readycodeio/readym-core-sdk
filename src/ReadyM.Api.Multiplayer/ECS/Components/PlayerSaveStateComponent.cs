using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// Marks an entity as part of a player's save and records where its state came from. Written by the server only. Not savable.
/// </summary>
[DeriveINetworkedComponent(emitDirtyMask: false)]
public partial struct PlayerSaveStateComponent : IServerAuthoritative
{
    private byte _dirtyMask;
    private byte _apiMask;

    /// <summary>Runtime id of the player whose save slot holds this entity.</summary>
    private PlayerId _owner;

    private PlayerStateOrigin _origin;
}
