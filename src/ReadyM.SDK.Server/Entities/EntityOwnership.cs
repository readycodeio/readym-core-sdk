using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Entities;

/// <summary>Which player's game runs an entity, so a handler can check that a client only reports on what it runs.</summary>
internal static class EntityOwnership
{
    /// <summary>The player whose game runs <paramref name="entity"/>, or <see cref="PlayerId.Server"/> when the server does.</summary>
    /// <returns>False for an entity that is gone or is not networked.</returns>
    internal static bool TryGetOwner(this EntityHandle entity, out PlayerId owner)
    {
        if (entity.IsAlive() && entity.TryGetComponent(out MetadataComponent meta))
        {
            owner = meta.Owner;
            return true;
        }

        owner = default;
        return false;
    }
}
