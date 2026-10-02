using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.SDK.Client.Entities;

namespace ReadyM.SDK.Client.Systems;

/// Answers <see cref="ModSystems.Running"/> once a tick. A game adds it to its update loop before
/// the groups holding mod systems.
public sealed class ModSystemGate(IEntities entities, ILogger logger) : BaseSystem
{
    private bool? _said;

    public override string Name => nameof(ModSystemGate);

    protected internal override void OnUpdateGroup() 
        => NotifyState(ModSystems.Refresh(entities));
    
    private void NotifyState(bool running)
    {
        if (_said == running)
            return;

        _said = running;

        if (running)
            logger.LogInformation("Mod systems are running.");
        else
            logger.LogWarning(
                "Mod systems are held: no World entity is in the ECS. Until one is, no [Service] "
                + "update runs. A game that never calls ApplyArchetypeExtensions leaves the World "
                + "archetype without the marker a query for it needs, and stays here forever.");
    }
}