using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client.State;

namespace ReadyM.Relay.Client.ECS.Systems;

internal class ClientSendEntityCreatedSystem(NetworkedComponentJobRegistry netJobRegistry, ClientState state, IRelayClient relay)
    : SendEntityCreatedSystemBase(netJobRegistry)
{
    protected override QueryFilter SetupFilter(QueryFilter filter, SendContext context)
    {
        filter = SetupBaseFilter(filter);

        if (context.IsGlobal)
            filter = filter.WithoutAnyComponents(ComponentTypes.Get<InScopeComponent>());
        else
            filter = filter.HasValue<InScopeComponent, Entity>(context.ScopeEntity!.Value);

        return filter;
    }

    protected override void CreatePacketHeader(NetDataWriter writer, SendContext context)
    {
        writer.PutMessageCode(RelayMessageCode.EcsCreateEntity);
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

        // A player's scope is global, like the player entity itself, so it does not wait for an area
        if (state.LocalPlayerEntry is { } localPlayer)
            base.OnUpdate(SendContext.FromPlayer(localPlayer.PlayerId, localPlayer.PlayerEntity));

        if (state is { CurrentAreaEntry: not null, LocalPlayerEntry: not null })
        {
            // Send area scoped updates
            {
                var scopeEntity = state.CurrentAreaEntry.Value.AreaEntity;
                var context = SendContext.FromArea(state.CurrentAreaEntry.Value.AreaId, scopeEntity);
                base.OnUpdate(context);
            }

            // Send cell scoped updates
            foreach (var cellEntry in state.ActiveCellEntries)
            {
                var context = SendContext.FromCell(new FullCellId(state.CurrentAreaId!.Value, cellEntry.CellId), cellEntry.CellEntity);
                base.OnUpdate(context);
            }
        }
    }
}