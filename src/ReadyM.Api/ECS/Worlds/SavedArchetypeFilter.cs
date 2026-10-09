using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Saves;

namespace ReadyM.Api.ECS.Worlds;

/// <summary>
/// Gives every archetype that declares a <see cref="SaveLayerComponent"/> a <see cref="PersistentIdComponent"/>.
/// </summary>
internal sealed class SavedArchetypeFilter : IArchetypeBuilderCallback
{
    public void AcceptComponentType<T>(ArchetypeBuilder builder) where T : struct, IComponent
        => AddPersistentIdFor<T>(builder);

    public void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue) where T : struct, IComponent
        => AddPersistentIdFor<T>(builder);

    public void AcceptStrideComponent(ArchetypeBuilder builder, int structIndex, int stride)
    {
        // A mod component.
    }

    public void AcceptTag<T>(ArchetypeBuilder builder) where T : struct, ITag
    {
    }

    private static void AddPersistentIdFor<T>(ArchetypeBuilder builder) where T : struct, IComponent
    {
        // The check keeps it to one id when the archetype declares it itself.
        if (typeof(T) == typeof(SaveLayerComponent) && !builder.Contains<PersistentIdComponent>())
            builder.Add<PersistentIdComponent>();
    }
}
