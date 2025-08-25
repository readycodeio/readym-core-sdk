using System;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Common.ECS.Components;

namespace ReadyM.Relay.Client.Shim;

public class ClientSynchronizerShimParserImpl(NetworkedEntityManager netEntity, ILogger logger) : ShimBuiltInMessageParserImplBase<ShimEcsDependencyData>
{
    public override bool SupportsRequest(ServerEventHeader header)
        => false;
    
    public override bool SupportsResponse(ServerEventHeader header)
        => header.EventCode is RelayMessageCode.EcsSnapshot or RelayMessageCode.EcsDelta or RelayMessageCode.EcsCreateEntity or RelayMessageCode.EcsDeleteEntity;

    public override ShimEcsDependencyData GetBuiltInRequestCustomData(ServerEventHeader header, NetDataReader reader)
        => throw new NotSupportedException();

    public override ShimEcsDependencyData GetBuiltInResponseCustomData(ServerEventHeader header, NetDataReader reader)
    {
        ShimEcsDependencyData GetDataForScopeEntity(Entity scopeEntity)
        {
            if (scopeEntity.TryGetComponent<AreaScopeComponent>(out var areaScope))
            {
                return new ShimEcsDependencyData
                {
                    AreaId = areaScope.AreaId,
                };
            }

            if (scopeEntity.TryGetComponent<PlayerScopeComponent>(out var playerScope))
            {
                return new ShimEcsDependencyData
                {
                    PlayerId = playerScope.PlayerId,
                };
            }
            
            logger.LogError("Entity {Id} is not a valid scope entity", scopeEntity);
            return default;
        }
        
        if (header.EventCode is RelayMessageCode.EcsSnapshot or RelayMessageCode.EcsCreateEntity)
        {
            var scopeNetId = reader.Get<NetworkId>();
            if (scopeNetId == default)
                return default;

            if (!netEntity.TryGetEntityByNetworkId(scopeNetId, out var scopeEntity))
            {
                logger.LogError("Failed to find scope entity {NetworkId} in ECS {EventCode}", scopeNetId, header.EventCode);
                return default;
            }

            return GetDataForScopeEntity(scopeEntity.Value);
        }
        else if (header.EventCode is RelayMessageCode.EcsDelta)
        {
            reader.Get<NetworkedComponentId>();
            var firstNetId = reader.Get<NetworkId>();

            if (!netEntity.TryGetEntityByNetworkId(firstNetId, out var firstEntity))
            {
                // NOTE: Deltas are unreliable so we might get that message early, or too late, etc.
                logger.LogWarning("Failed to find entity {NetworkId} in ECS {EventCode}", firstNetId, header.EventCode);
                return default;
            }

            if (!firstEntity.Value.TryGetComponent<InScopeComponent>(out var inScopeComp))
                // NOTE: global scope
                return default;

            return GetDataForScopeEntity(inScopeComp.ScopeEntity);
        }
        else if (header.EventCode is RelayMessageCode.EcsDeleteEntity)
        {
            reader.Get<NetworkedComponentId>();
            var firstNetId = reader.Get<NetworkId>();

            if (!netEntity.TryGetEntityByNetworkId(firstNetId, out var firstEntity))
            {
                // NOTE: Deltas are unreliable so we might get that message early, or too late, etc.
                logger.LogWarning("Failed to find entity {NetworkId} in ECS {EventCode}", firstNetId, header.EventCode);
                return default;
            }

            if (!firstEntity.Value.TryGetComponent<InScopeComponent>(out var inScopeComp))
                // NOTE: global scope
                return default;

            return GetDataForScopeEntity(inScopeComp.ScopeEntity);
        }
        else
        {
            logger.LogError("Unsupported event code {EventCode} for built-in response data", header.EventCode);
            return default;
        }
    }
}