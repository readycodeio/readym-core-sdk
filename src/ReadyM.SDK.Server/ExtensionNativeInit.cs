using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server;

internal static class ExtensionNativeInit
{
    private static Type[]? _initialise;

    private static Type[]? _created;

    private static IEntityApi? _api;

    public static void Use(IEntityApi api) => _api = api;

    /// Called for every entity of a built-in archetype.
    /// <param name="local">False for an entity that was created elsewhere and was replicated here.</param>
    public static void Run(RawEntity entity, IComponentsById components, bool local)
    {
        _initialise ??= Candidates(NativeInitRegistry.Needs);

        foreach (var component in _initialise)
            NativeInitRegistry.InitIfPresent(entity.Id, components, component);

        // Do not run create handlers for entities that were created elsewhere and were replicated here.
        if (!local || !CreateHandlerRegistry.Any || _api is null)
            return;

        _created ??= [.. CreateHandlerRegistry.Components];

        var handle = new EntityHandle(entity, _api);

        foreach (var component in _created)
            CreateHandlerRegistry.RunIfPresent(handle, components, component);
    }

    /// Worked out once, after every mod has registered its shapes.
    private static Type[] Candidates(Func<Type, bool> wanted)
    {
        var found = new List<Type>();

        foreach (var shape in ArchetypeContributions.Shapes())
        {
            foreach (var component in ArchetypeContributions.OwnGenerated(shape))
                if (wanted(component) && !found.Contains(component))
                    found.Add(component);

            foreach (var component in ArchetypeRegistry.GeneratedAddedTo(shape))
                if (wanted(component) && !found.Contains(component))
                    found.Add(component);
        }

        return [.. found];
    }
}