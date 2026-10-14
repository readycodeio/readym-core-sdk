using ReadyM.SDK.Archetypes;
using IComponentRegistry = ReadyM.Relay.Server.Sdk.Ecs.Components.IComponentRegistry;
using HostComponents = ReadyM.Relay.Server.Sdk.Ecs.Components.ModComponentRegistry;

namespace ReadyM.SDK.Server;

/// Tells the server what a mod's shapes add to the archetypes the game registers.
public static class ServerArchetypes
{
    /// <summary>Whether the replication pass owns this component rather than this one.</summary>
    private static bool Replicates(Type component)
        => typeof(ReadyM.Api.Multiplayer.ECS.Components.INetworkedComponent).IsAssignableFrom(component);

    /// Returns how many components were handed over, which a mod can log to see its shapes arrived.
    public static int ApplyExtensions(IComponentRegistry registry)
    {
        if (registry is not HostComponents host)
            return 0;

        var applied = 0;

        var registered = new HashSet<Type>();

        foreach (var shape in ArchetypeContributions.Shapes())
        {
            foreach (var component in ArchetypeContributions.OwnGenerated(shape))
            {
                if (registered.Add(component) && !Replicates(component))
                    host.RegisterLocalComponent(component);
            }

            var contributed = Contributed(shape);

            if (contributed.Count == 0)
                continue;

            host.AddArchetypeExtensions(shape, contributed);
            applied += contributed.Count;
        }

        return applied;
    }

    /// Every component a mod adds to the game's archetype for a shape.
    internal static List<Type> Contributed(Type shape)
    {
        var contributed = new List<Type>(ArchetypeContributions.OwnGenerated(shape));
        
        contributed.AddRange(ArchetypeRegistry.GeneratedAddedTo(shape));
        
        foreach (var change in contributed.Where(Replicates).Select(ChangeComponents.Of).OfType<Type>().ToList())
        {
            if (!contributed.Contains(change))
                contributed.Add(change);
        }

        return contributed;
    }
}
