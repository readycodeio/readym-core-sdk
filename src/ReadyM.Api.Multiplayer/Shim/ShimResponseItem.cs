using System.Collections.Generic;
using System.Text.Json.Serialization;
using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Shim;

internal struct ShimResponseItem
{
    [JsonPropertyName("elapsed")]
    public long Elapsed { get; set; } 
    
    [JsonPropertyName("kind")]
    public ShimResponseKind Kind { get; set; } 
    
    [JsonPropertyName("disconnectReason")]
    public DisconnectedReason DisconnectedReason { get; set; }

    [JsonPropertyName("playerId")]
    public PlayerId PlayerId { get; set; }

    [JsonPropertyName("nextId")]
    public uint NextId { get; set; }

    [JsonPropertyName("otherPlayers")]
    public List<PlayerId>? OtherPlayers { get; set; }

    [JsonPropertyName("areaId")]
    public AreaId AreaId { get; set; }
    
    [JsonPropertyName("ping")]
    public int Ping { get; set; }

    [JsonPropertyName("serverHeader")]
    public ServerEventHeader ServerHeader { get; set; }

    [JsonPropertyName("clientHeader")]
    public CustomRelayEventHeader ClientHeader { get; set; }
    
    [JsonPropertyName("rawData")]
    public ShimBuffer RawData { get; set; }
    
    [JsonPropertyName("customData")]
    public object? CustomData { get; set; }
    
    public RelayMessageCode? EventCode
        => Kind switch
        {
            ShimResponseKind.AnyBuiltInMessage => ServerHeader.EventCode,
            ShimResponseKind.AnyServerMessage => ServerHeader.EventCode,
            ShimResponseKind.AnyClientMessage => ClientHeader.EventCode,
            _ => null
        };
    
    public T GetCustomData<T>()
        where T : struct
        => CustomData is not null ? (T)CustomData : default;
}
