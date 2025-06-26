using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

public abstract class ReadyMultiplayerMod : ReadyMod, IDisposable
{
    internal readonly NetworkedEntityManager NetManager;

    // TODO: RelayClient has all the lifetime events, make other methods internal
    public RelayClient RelayClient { get; }

    internal readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> SnapshotReaderJobs = [];
    internal readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> DeltaReaderJobs = [];

    protected ReadyMultiplayerMod(Guid userGuid, string host, int port)
    {
        RelayClient = new RelayClient(userGuid, host, port, Log);
        NetManager = new NetworkedEntityManager(World, () => RelayClient.PlayerId);
        Configure();
    }

    // For testing, TODO: Inject dependencies
    protected internal ReadyMultiplayerMod(RelayClient client)
    {
        RelayClient = client;
        NetManager = new NetworkedEntityManager(World, () => RelayClient.PlayerId);
        Configure();
    }

    private void Configure()
    {
        NetManager.OnEntityDeleted += HandleEntityDeleted;

        RelayClient.OnReceivedDeleteEntity += DeleteRemoteEntityFromEcs;
        RelayClient.OnPingUpdated += OnPingUpdated;
        RelayClient.OnCustomEvent += OnCustomEvent;
        RelayClient.OnEcsDelta += OnEcsDelta;
        RelayClient.OnEcsSnapshot += OnEcsSnapshot;
    }

    protected abstract void OnCustomEvent(CustomEventHeader header, NetPacketReader reader);

    private void OnEcsDelta(NetDataReader reader)
    {
        var componentId = reader.Get<NetworkedComponentId>();

        if (!DeltaReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
        {
            Log(LogLevel.Error, "No delta reader job registered for component ID: {Id}", componentId);
            return;
        }

        var bytesToCopy = reader.GetRemainingBytes();
        var readerCopy = new NetDataReader(bytesToCopy, 0, bytesToCopy.Length);

        RunOnGameThread(() => { readerJob.Execute(readerCopy); });
    }

    private void OnEcsSnapshot(NetDataReader reader)
    {
        var bytesToCopy = reader.GetRemainingBytes();
        var readerCopy = new NetDataReader(bytesToCopy, 0, bytesToCopy.Length);

        RunOnGameThread(() =>
        {
            while (readerCopy.TryGetNetworkedComponentId(out var componentId))
            {
                if (!SnapshotReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
                {
                    Log(LogLevel.Error, "No snapshot reader job registered for component ID: {Id}", componentId);
                    return;
                }

                readerJob.Execute(readerCopy);
            }
        });
    }

    #region Lifetime

    protected abstract void ConfigureNetworking(INetworkedComponentConfig config);

    protected abstract void RunOnGameThread(Action action);

    public override void Initialize()
    {
        base.Initialize();

        var builder = new NetworkedComponentConfig(this);
        ConfigureNetworking(builder);

        RelayClient.Start();
    }

    public override void Deinitialize()
    {
        base.Deinitialize();
        
        SnapshotReaderJobs.Clear();
        DeltaReaderJobs.Clear();
        
        RelayClient.Stop();
    }

    public virtual void EnterRoom()
    {
        RelayClient.SendInitialPlayerState();
    }

    public virtual void ExitRoom() { }

    protected virtual void OnPingUpdated(int ping) { }

    #endregion

    #region ECS

    public (Entity Entity, NetworkIdComponent NetId) CreateNetworkedEntity(ArchetypeId archetype, Action<EntityBuilder>? setComponents = null)
    {
        var (entity, netId) = NetManager.CreateNetworkedEntity(archetype, setComponents);
        Log(LogLevel.Debug, "Networked entity created: {Id} (owned)", netId);
        return (entity, netId);
    }

    protected bool TryGetEntityByNetworkId(NetworkIdComponent netEntity, [NotNullWhen(true)] out Entity? entity)
    {
        return NetManager.TryGetEntityByNetworkId(netEntity, out entity);
    }

    private void HandleEntityDeleted(NetworkIdComponent netId)
    {
        if (netId.Creator == RelayClient.LocalPlayer.PlayerId)
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
        Console.WriteLine($"[{level}] {message} {string.Join(", ", args)}");
    }

    public virtual void Dispose()
    {
        NetManager.OnEntityDeleted -= HandleEntityDeleted;
        NetManager.Dispose();

        RelayClient.OnReceivedDeleteEntity -= DeleteRemoteEntityFromEcs;
        RelayClient.OnPingUpdated -= OnPingUpdated;
        RelayClient.OnCustomEvent -= OnCustomEvent;
        RelayClient.Dispose();
    }
}