using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

internal static class EntityExtensions
{
    extension(Entity self)
    {
        public NetworkId GetNetId()
            => self.GetComponent<MetadataComponent>().NetId;

        public ref MetadataComponent GetMeta()
            => ref self.GetComponent<MetadataComponent>();
    }
}