using Friflo.Engine.ECS;
using ReadyM.Api.Generators;

namespace ReadyM.Api.Saves;

/// <summary>
/// Declares which <see cref="SaveLayer"/> an entity is saved into.
/// </summary>
/// <remarks>Not networked: it only matters to the server's save.</remarks>
[DeriveSavableComponent]
public partial struct SaveLayerComponent : IComponent
{
    private SaveLayer _layer;

    public SaveLayerComponent(SaveLayer layer) => _layer = layer;

    public SaveLayer Layer
    {
        readonly get => _layer;
        set => _layer = value;
    }
}
