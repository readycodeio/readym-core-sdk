using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using System.Collections.Generic;

namespace ReadyM.Api.Multiplayer.Shim;

internal struct ShimRequestItem
{
    public ShimRequestKind Kind;
    public AreaId AreaId;
    public List<CellId> CellIds;
    
    public ServerEventHeader ServerHeader;
    public CustomRelayEventHeader ClientHeader;
    public ShimBuffer RawData;
    public object? CustomData;

    public RelayMessageCode? EventCode
        => Kind switch
        {
            ShimRequestKind.SentBuiltInMessage => ServerHeader.EventCode,
            ShimRequestKind.SentServerRpcMessage => ServerHeader.EventCode,
            ShimRequestKind.SentClientRpcMessage => ClientHeader.EventCode,
            _ => null
        };

    public T GetCustomData<T>()
        where T : struct
        => CustomData is not null ? (T)CustomData : default;
}