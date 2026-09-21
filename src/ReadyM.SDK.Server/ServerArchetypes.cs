using ReadyM.SDK.Archetypes;
using IComponentRegistry = ReadyM.Relay.Server.Sdk.Ecs.Components.IComponentRegistry;
using HostComponents = ReadyM.Relay.Server.Sdk.Ecs.Components.ComponentRegistry;

namespace ReadyM.SDK.Server;

/// Tells the server what a mod's shapes add to the archetypes the game registers.
public static class ServerArchetypes
{
    /// Returns how many components were handed over, which a mod can log to see its shapes arrived.
    public static int ApplyExtensions(IComponentRegistry registry)
    {
        if (registry is not HostComponents host)
            return 0;

        var applied = 0;

        foreach (var shape in ArchetypeContributions.Shapes())
        {
            var contributed = new List<Type>();

            // What the shape itself brought.
            foreach (var component in ArchetypeContributions.OwnGenerated(shape))
            {
                host.RegisterLocalComponent(component);
                contributed.Add(component);
            }

            // What mods added with [Extends], again only what they generated. A mixin sitting on
            // a component the game already declares is the old pipeline's to place, not ours.
            contributed.AddRange(ArchetypeRegistry.GeneratedAddedTo(shape));

            if (contributed.Count == 0)
                continue;

            host.AddArchetypeExtensions(shape, contributed);
            applied += contributed.Count;
        }

        return applied;
    }
}
