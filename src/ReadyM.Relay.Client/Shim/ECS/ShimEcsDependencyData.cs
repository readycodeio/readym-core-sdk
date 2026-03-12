using ReadyM.Api.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Relay.Client.Shim.ECS;

[DeriveJsonSerializable]
internal partial struct ShimEcsDependencyData(AreaId areaId, PlayerId playerId)
{
    private AreaId _areaId = areaId;
    private PlayerId _playerId = playerId;
    
    public AreaId? AreaId
    {
        get => _areaId == Api.Idents.AreaId.Invalid ? null : _areaId;
        set => _areaId = value ?? Api.Idents.AreaId.Invalid;
    }

    public PlayerId? PlayerId
    {
        get => _playerId == Api.Idents.PlayerId.Invalid ? null : _playerId;
        set => _playerId = value ?? Api.Idents.PlayerId.Invalid;
    }
}