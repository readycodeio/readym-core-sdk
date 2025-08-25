using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Relay.Client.Shim;

[DeriveJsonSerializable]
public partial struct ShimEcsDependencyData(AreaId areaId, PlayerId playerId)
{
    private AreaId _areaId = areaId;
    private PlayerId _playerId = playerId;
    
    public AreaId? AreaId
    {
        get => _areaId == Api.Multiplayer.Idents.AreaId.Invalid ? null : _areaId;
        set => _areaId = value ?? Api.Multiplayer.Idents.AreaId.Invalid;
    }

    public PlayerId? PlayerId
    {
        get => _playerId == Api.Multiplayer.Idents.PlayerId.Invalid ? null : _playerId;
        set => _playerId = value ?? Api.Multiplayer.Idents.PlayerId.Invalid;
    }
}