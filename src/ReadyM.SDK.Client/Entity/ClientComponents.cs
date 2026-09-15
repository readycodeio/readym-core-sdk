using System.Collections.Concurrent;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Entity;

/// <summary>
/// Resolves a <see cref="ComponentSet"/> to the store's own component identity.
/// </summary>
internal static class ClientComponents
{
    private static readonly ConcurrentDictionary<ComponentSet, ComponentTypes> Resolved = new();

    public static ComponentTypes Resolve(ComponentSet components) => Resolved.GetOrAdd(components, static set =>
    {
        var schema = EntityStore.GetEntitySchema();
        var types = default(ComponentTypes);

        foreach (var type in set.Types)
        {
            if (!schema.ComponentTypeByType.TryGetValue(type, out var componentType))
                throw new InvalidOperationException($"{type.FullName} is not a registered component type.");

            types.Add(new ComponentTypes(componentType));
        }

        return types;
    });
}
