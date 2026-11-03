using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Client.Entities;

namespace ReadyM.SDK.Client.Systems;

internal static class ModSystems
{
    /// <summary>
    /// Whether mod systems are allowed to run this frame.
    /// False when disconnected or the world entity is not present.
    /// </summary>
    public static bool Running { get; private set; }

    /// <summary>Refreshes the running state based on the presence of the world entity.</summary>
    public static bool Refresh(IEntities entities) => Running = HasWorld(entities);

    private static bool HasWorld(IEntities entities)
    {
        foreach (var _ in entities.Query<World>())
            return true;

        return false;
    }
}