using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

public class ReadyMultiplayerMod: ReadyMod, IDisposable
{
    public readonly NetworkedEntityManager NetManager;

    // TODO: RelayClient has all the lifetime events, make other methods internal
    public RelayClient RelayClient { get; }

    public ReadyMultiplayerMod(Guid userGuid, string host, int port)
    {
        RelayClient = new RelayClient(userGuid, host, port, Log);
        NetManager = new NetworkedEntityManager(World, () => RelayClient.PeerId);
        Configure();
    }

    protected ReadyMultiplayerMod(RelayClient client)
    {
        RelayClient = client;
        NetManager = new NetworkedEntityManager(World, () => RelayClient.PeerId);
        Configure();
    }

    private void Configure()
    {
        NetManager.onEntityDeleted += HandleEntityDeleted;

        RelayClient.OnReceivedDeleteEntity += DeleteRemoteEntityFromEcs;
        RelayClient.OnPingUpdated += OnPingUpdated;
        RelayClient.OnCustomEvent += OnCustomEvent;
    }

    public bool IsMasterClient => (short)RelayClient.RoomState.GetValueOrDefault(RoomProperties.MasterClientId, Constants.UnsetPeerId) == RelayClient.PeerId;

    protected virtual void OnCustomEvent(CustomEventHeader header, NetPacketReader reader) { }

    #region Lifetime

    public void Start()
    {
        RelayClient.Start();
    }

    public void Stop()
    {
        RelayClient.Stop();
    }

    protected virtual void OnPingUpdated(int ping) { }

    #endregion
    
    #region ECS

    private void HandleEntityDeleted(NetworkIdComponent netId)
    {
        if (netId.Owner == RelayClient.LocalPlayer.PeerId)
        {
            // our own entity - send destroy event
            Log(LogLevel.Debug, "Networked entity destroyed: {Id} (owned)", netId);
            var writer = new NetDataWriter();
            writer.Put((byte)SystemEvent.DestroyEntity);
            writer.Put(netId);
            RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.ReliableOrdered); // TODO: Use RPC API instead
        }
    }

    private void DeleteRemoteEntityFromEcs(NetworkIdComponent netId)
    {
        if (NetManager.TryGetEntityByNetworkId(netId, out var entity))
        {
            Log(LogLevel.Debug, "Queueing remote entity for destruction: {Id}", netId);
            CommandBuffer.DeleteEntity(entity.Value.Id);
        }
        else
        {
            Log(LogLevel.Error, "Received destroy event for locally non-existent entity: {Id}", netId);
        }
    }

    #endregion

    protected virtual void Log(LogLevel level, [StructuredMessageTemplate] string message, params object?[] args)
    {
        Console.WriteLine($"[{level}] {string.Format(message, args)}");
    }

    public virtual void Dispose()
    {
        NetManager.onEntityDeleted -= HandleEntityDeleted;
        NetManager.Dispose();

        RelayClient.OnReceivedDeleteEntity -= DeleteRemoteEntityFromEcs;
        RelayClient.OnPingUpdated -= OnPingUpdated;
        RelayClient.OnCustomEvent -= OnCustomEvent;
        RelayClient.Dispose();
    }
}