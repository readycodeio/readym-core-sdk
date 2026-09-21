using System.ComponentModel;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Archetypes;

/// Reaching an entity's components by its id, for the one case that has no handle: an entity the
/// host built and is telling the SDK about.
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IComponentsById
{
    bool Has<T>(int entityId) where T : struct, IComponent;

    ref T Ref<T>(int entityId) where T : struct, IComponent;
}
