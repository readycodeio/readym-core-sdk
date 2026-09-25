using Friflo.Engine.ECS.Systems;
using ReadyM.SDK.Client.Entities;

namespace ReadyM.SDK.Client.Systems;

/// Answers <see cref="ModSystems.Running"/> once a tick. A game adds it to its update loop before
/// the groups holding mod systems.
public sealed class ModSystemGate(IEntities entities) : BaseSystem
{
    public override string Name { get; } = nameof(ModSystemGate);

    // Asked every tick rather than driven by a disconnect event: a reconnect that does not raise
    // one would otherwise leave the systems running against an ECS that no longer holds the entity
    // they are about to read.
    protected internal override void OnUpdateGroup() => ModSystems.Refresh(entities);
}