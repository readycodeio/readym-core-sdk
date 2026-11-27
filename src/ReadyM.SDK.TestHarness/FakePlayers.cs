using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Players;

namespace ReadyM.SDK.TestHarness;

/// <summary>Stands in for the relay's player list, with nobody in any area or cell.</summary>
internal sealed class FakePlayers : IPlayers
{
    public void Kick(PlayerId player) => throw new NotSupportedException();

    public Guid? GetReadyMId(PlayerId player) => throw new NotSupportedException();

    public List<PlayerId> InArea(AreaId area) => [];

    public List<PlayerId> InCell(AreaId area, CellId cell) => [];

    public List<PlayerId> InArea(FullAreaId area) => [];

    public List<PlayerId> InCell(FullCellId cell) => [];
}
