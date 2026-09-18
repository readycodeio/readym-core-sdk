using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Values a mod adds to somebody else's archetype, read straight off it.
/// </summary>
/// <remarks>
/// The extension lives in the assembly that declared the mixin, so only code referencing that mod
/// sees the members. That is what lets a client-only mixin sit on a shared archetype without the
/// server half knowing about it.
/// </remarks>
public class ExtendedMemberTests : ClientSdkTest
{
    [Fact]
    public void An_added_value_reads_off_the_archetype()
    {
        var area = Entities.Create<CoreArea>();

        area.SetTemperature(21);

        Assert.Equal(21, area.Temperature);
    }

    [Fact]
    public void An_added_value_is_the_one_the_mixin_holds()
    {
        var area = Entities.Create<CoreArea>();

        area.SetTemperature(5);

        Assert.True(EntityHandleOf(area).TryAs<Climate>(out var climate));
        Assert.Equal(5, climate.Temperature);
    }

    /// A getter-only member on the mixin stays getter-only on the archetype.
    [Fact]
    public void A_value_without_a_setter_is_read_only_on_the_archetype()
    {
        var area = Entities.Create<CoreArea>();

        Assert.Equal(0, area.Rainfall);
    }

    /// The prefix is what keeps two mods from colliding on one name.
    [Fact]
    public void A_prefixed_value_takes_the_name_the_mod_chose()
    {
        var area = Entities.Create<CoreArea>();

        area.SetTemperature(1);
        area.SetAmbientTemperature(2);

        Assert.Equal(1, area.Temperature);
        Assert.Equal(2, area.AmbientTemperature);
    }

    /// <summary>
    /// The same members on a chunk view, reached through the entity because the value is not in the
    /// archetype's own chunk.
    /// </summary>
    [Fact]
    public void An_added_value_reads_off_a_chunk_view_too()
    {
        Entities.Create<CoreArea>().SetTemperature(9);

        var seen = 0;

        foreach (var area in Entities.Query<CoreArea>())
        {
            seen += area.Temperature;
            area.SetAmbientTemperature(3);
        }

        Assert.Equal(9, seen);

        foreach (var area in Entities.Query<CoreArea>())
            Assert.Equal(3, area.AmbientTemperature);
    }

    /// What the remarks on the generated member point at, and it has to agree.
    [Fact]
    public void Naming_the_mixin_in_the_query_reads_the_same_value()
    {
        Entities.Create<CoreArea>().SetTemperature(12);

        foreach (var (area, climate) in Entities.Query<CoreArea, Climate>())
        {
            Assert.Equal(12, climate.Temperature);
            Assert.Equal(12, area.Temperature);
        }
    }

    private static EntityHandle EntityHandleOf<T>(T shape)
        where T : struct, IArchetypeQueryable
        => EntityHandle.Of(shape);
}
