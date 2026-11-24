using ReadyM.Api.Idents;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Making an entity a resident, asking whether it is one, and ending it. The server refuses an entity that is in no
/// scope, and the SDK turns that refusal into an exception.
/// </summary>
public class ServerResidentTests : ServerSdkTest
{
    private static readonly FullCellId Market = new(new AreaId("market"), new CellId("interior"));

    private TestArea Area()
    {
        var area = Entities.Create<TestArea>();
        area.AreaId = 1;
        return area;
    }

    [Fact]
    public void An_entity_in_a_scope_becomes_a_resident_and_stops_being_one()
    {
        var guard = Entities.Create<Guard>(Area());
        Assert.False(Entities.IsResident(guard));

        Entities.MakeResident(guard);
        Assert.True(Entities.IsResident(guard));

        Entities.EndResidency(guard);
        Assert.False(Entities.IsResident(guard));
    }

    [Fact]
    public void Only_that_entity_becomes_a_resident()
    {
        var area = Area();
        var guard = Entities.Create<Guard>(area);
        var other = Entities.Create<Guard>(area);

        Entities.MakeResident(guard);

        Assert.True(Entities.IsResident(guard));
        Assert.False(Entities.IsResident(other));
    }

    [Fact]
    public void An_entity_in_no_scope_cannot_become_a_resident()
    {
        var guard = Entities.Create<Guard>();

        Assert.Throws<InvalidOperationException>(() => Entities.MakeResident(guard));
        Assert.False(Entities.IsResident(guard));
    }

    [Fact]
    public void An_entity_in_no_scope_becomes_a_resident_of_a_named_cell()
    {
        var guard = Entities.Create<Guard>();
        var other = Entities.Create<Guard>();

        Entities.MakeResident(guard, Market);

        Assert.True(Entities.IsResident(guard));
        Assert.False(Entities.IsResident(other));
    }

    [Fact]
    public void An_entity_in_a_scope_cannot_become_a_resident_of_a_named_cell()
    {
        var guard = Entities.Create<Guard>(Area());

        Assert.Throws<InvalidOperationException>(() => Entities.MakeResident(guard, Market));
        Assert.False(Entities.IsResident(guard));
    }

    [Fact]
    public void A_resident_cannot_become_a_resident_of_a_named_cell_again()
    {
        var guard = Entities.Create<Guard>();
        Entities.MakeResident(guard, Market);

        Assert.Throws<InvalidOperationException>(() => Entities.MakeResident(guard, Market));
        Assert.True(Entities.IsResident(guard));
    }

    [Fact]
    public void Ending_a_residency_that_never_began_throws()
    {
        var guard = Entities.Create<Guard>(Area());

        Assert.Throws<InvalidOperationException>(() => Entities.EndResidency(guard));
    }
}
