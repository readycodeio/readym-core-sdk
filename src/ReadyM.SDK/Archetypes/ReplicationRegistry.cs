using System.Collections.Concurrent;
using System.ComponentModel;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Archetypes;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ReplicationRegistry
{
    private static readonly ConcurrentDictionary<Type, Delivery> Deliveries = new();

    /// Called by generated code for a replicated shape.
    public static void Register<TComponent>(Delivery delivery)
        where TComponent : struct, Friflo.Engine.ECS.IComponent
        => Deliveries[typeof(TComponent)] = delivery;

    public static IReadOnlyList<ReplicatedComponent> Snapshot()
        => [.. Deliveries.Select(entry => new ReplicatedComponent(entry.Key, entry.Value))];

    /// Only for tests, which need each case to start from nothing.
    internal static void Clear() => Deliveries.Clear();
}
