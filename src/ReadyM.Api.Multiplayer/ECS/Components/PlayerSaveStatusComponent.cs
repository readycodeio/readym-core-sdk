using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// Set by the owning client once the entity's state is in the game (from a save) or in ECS (from the
/// game). The server saves a player-save entity only once this is set.
/// </summary>
[DeriveINetworkedComponent(emitDirtyMask: false)]
public partial struct PlayerSaveStatusComponent : IOwnershipBased
{
    private byte _dirtyMask;
    private byte _apiMask;

    private bool _applied;
}
