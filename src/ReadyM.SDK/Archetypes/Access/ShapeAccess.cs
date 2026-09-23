using ReadyM.Api.Mapping.Tags;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes.Access;

internal sealed class ShapeAccess<TComponent> : ShapeAccess
    where TComponent : struct, IReadyComponent
{
    internal override Type Component { get; } = typeof(TComponent);

    internal override bool WasOverridden(in EntityHandle handle)
        => handle.GetComponent<TComponent>().ChangedFromApi;

    internal override void Applied(in EntityHandle handle) => handle.GetComponent<TComponent>().ClearApiFlag();
}

internal abstract class ShapeAccess
{
    internal abstract Type Component { get; }

    internal abstract bool WasOverridden(in EntityHandle handle);

    internal abstract void Applied(in EntityHandle handle);
}