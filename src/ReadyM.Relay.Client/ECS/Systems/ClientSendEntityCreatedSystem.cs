using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Jobs;
using ReadyM.Relay.Common.ECS.Systems;

namespace ReadyM.Relay.Client.ECS.Systems;

public class ClientSendEntityCreatedSystem(JobRegistry jobRegistry, ClientState state, IRelayClient relay)
    : SendEntityCreatedSystemBase(jobRegistry)
{
    protected override QueryFilter SetupFilter(QueryFilter filter, SendContext context)
    {
        filter = base.SetupFilter(filter, context);

        if (context.IsGlobal)
            filter = filter.WithoutAnyComponents(ComponentTypes.Get<InScopeComponent>());
        else
            filter = filter.HasValue<InScopeComponent, Entity>(context.ScopeEntity!.Value);

        return filter;
    }

    protected override void CreatePacketHeader(NetDataWriter writer, SendContext context)
    {
        writer.Put((byte)RelayMessageCode.EcsCreateEntity);
        if (context.ScopeEntity != null)
        {
            var meta = context.ScopeEntity.Value.GetComponent<MetadataComponent>();
            writer.Put(meta.NetId);
        }
        else
        {
            writer.Put(default(NetworkId));
        }
    }

    protected override void Send(NetDataWriter data, SendContext context)
    {
        relay.SendRawMessage(data, DeliveryMethod.ReliableOrdered);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate(SendContext.Global);

        if (state is { CurrentAreaEntry: not null, LocalPlayerEntry: not null })
        {
            // Send player scoped updates
            {
                var scopeEntity = state.LocalPlayerEntry.Value.PlayerEntity;
                var context = SendContext.FromPlayer(state.LocalPlayerEntry.Value.PlayerId, scopeEntity);
                base.OnUpdate(context);
            }

            // Send area scoped updates
            {
                var scopeEntity = state.CurrentAreaEntry.Value.AreaEntity;
                var context = SendContext.FromArea(state.CurrentAreaEntry.Value.AreaId, scopeEntity);
                base.OnUpdate(context);
            }
        }
    }
}