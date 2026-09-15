using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Entity;

/// Resolves a <see cref="ComponentSet"/> to the store's component bitset.
internal static class ClientComponents
{
    private static readonly Dictionary<ComponentSet, ComponentTypes> Resolved = new();

    public static ComponentTypes Resolve(ComponentSet components)
    {
        if (Resolved.TryGetValue(components, out var types))
            return types;

        var schema = EntityStore.GetEntitySchema();

        foreach (var type in components.Types)
        {
            if (!schema.ComponentTypeByType.TryGetValue(type, out var componentType))
                throw new InvalidOperationException($"{type.FullName} is not a registered component type.");

            types.Add(new ComponentTypes(componentType));
        }

        Resolved[components] = types;
        return types;
    }
}
