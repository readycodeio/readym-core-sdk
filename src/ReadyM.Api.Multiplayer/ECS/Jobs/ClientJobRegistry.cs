using System.Collections.Generic;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.Client;

public class ClientJobRegistry
{
    public readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> SnapshotReaderJobs = [];
    public readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> DeltaReaderJobs = [];
}