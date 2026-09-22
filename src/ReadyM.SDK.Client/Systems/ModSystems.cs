using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;

namespace ReadyM.SDK.Client.Systems;

public static class ModSystems
{
    /// Whether mod systems are allowed to run this frame.
    /// False when disconnected or the world entity is not present.
    public static bool Running { get; private set; }

    /// Refreshes the running state based on the presence of the world entity.
    public static bool Refresh(IEntities entities) => Running = HasWorld(entities);

    private static bool HasWorld(IEntities entities)
    {
        foreach (var _ in entities.Query<World>())
            return true;

        return false;
    }
}