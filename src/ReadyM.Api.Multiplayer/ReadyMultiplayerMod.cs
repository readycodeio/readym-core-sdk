using System;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

public class ReadyMultiplayerMod : ReadyMod, IDisposable
{
    private readonly NetworkedEntityManager _netManager;
    
    // TODO: RelayClient has all the lifetime events, make other methods internal
    public RelayClient RelayClient { get; }

    public ReadyMultiplayerMod(Guid userGuid, string host, int port)
    {
        RelayClient = new RelayClient(userGuid, host, port, Log);
        _netManager = new NetworkedEntityManager(World, Constants.UnsetPeerId); // TODO: This needn't be set in the constructor
        _netManager.onEntityDeleted += HandleEntityDeleted;
        RelayClient.OnReceivedDestroyEntity += DeleteRemoteEntityFromEcs;
    }

    [Obsolete("Waiting for player/room state refactoring")]
    private void Configure()
    {
        RelayClient.RegisterType(typeof(NetworkIdComponent), (writer, customObject) =>
        {
            var id = (NetworkIdComponent)customObject;
            writer.Put(id.Id);
        }, reader => reader.GetNetworkId());
    }

    private bool IsMasterClient => RelayClient.PeerId == 0; // TODO

    /// <summary>
    /// Is it safe to run client patches?
    /// </summary>
    public bool ConnectedAndInRoom => RelayClient.InRoom;

    #region Lifetime

    public void Start()
    {
        RelayClient.Start();
    }

    public void Stop()
    {
        RelayClient.Stop();
    }
    
    private void UpdatePeerId()
    {
        Log(LogLevel.Debug, "Updating NetManager peer id to {PeerId}", RelayClient.PeerId);
        _netManager.PeerId = RelayClient.PeerId;
    }

    #endregion

    // TODO: RPC API

    #region RPC

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
        if (_netManager.TryGetEntityByNetworkId(netId, out var entity))
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

    public void Dispose()
    {
        _netManager.onEntityDeleted -= HandleEntityDeleted;
        _netManager.Dispose();
        RelayClient.Dispose();
    }
}