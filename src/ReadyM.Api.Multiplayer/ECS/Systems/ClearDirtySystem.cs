using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public class ClearDirtySystem<T> : QuerySystem<T>
    where T : struct, INetworkedComponent
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref comp, entity) =>
        {
            comp.ClearDirty();
        });
    }
}