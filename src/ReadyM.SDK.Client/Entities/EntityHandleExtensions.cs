using Friflo.Engine.ECS;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Client.Entities;

internal static class EntityHandleExtensions
{
    extension(EntityHandle handle)
    {
        internal Entity ToEntity(EntityStore store)
            => store.GetEntityByRawEntity(handle.RawEntity);
    }
}