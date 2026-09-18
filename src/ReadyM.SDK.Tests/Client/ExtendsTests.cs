using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Mods adding mixins to an archetype somebody else declared.
/// </summary>
/// <remarks>
/// Two mods extend CoreArea here, which is the case that used to need a third archetype including
/// both. Extending changes what creating an archetype carries, and nothing else: a query for it
/// still matches an entity that carries none of the additions.
/// </remarks>
public class ExtendsTests : ClientSdkTest
{
    [Fact]
    public void Creating_the_archetype_carries_every_extension()
    {
        var area = Entities.Create<CoreArea>();
        var handle = EntityHandle.Of(area);

        Assert.True(handle.Is<Weather>());
        Assert.True(handle.Is<Music>());
    }

    [Fact]
    public void An_extension_is_readable_and_writable_through_its_own_shape()
    {
        var area = Entities.Create<CoreArea>();

        Assert.True(EntityHandle.Of(area).TryAs<Weather>(out var weather));

        weather.Storm = 3;

        Assert.Equal(3, weather.Storm);
    }

    /// Two mods extending the same archetype both land, neither knowing about the other.
    [Fact]
    public void Extensions_from_two_mods_both_land()
    {
        var area = Entities.Create<CoreArea>();

        Assert.True(EntityHandle.Of(area).TryAs<Weather>(out var weather));
        Assert.True(EntityHandle.Of(area).TryAs<Music>(out var music));

        weather.Storm = 1;
        music.Track = 2;

        Assert.Equal(1, weather.Storm);
        Assert.Equal(2, music.Track);
    }

    /// <summary>
    /// The point of extending rather than redeclaring: what the archetype matches is unchanged, so
    /// an entity carrying none of the additions is still found.
    /// </summary>
    [Fact]
    public void Extending_does_not_narrow_what_the_archetype_matches()
    {
        Assert.Equal(1, CoreAreaAccessors.Components.Count);

        var bare = Store.CreateEntity();

        bare.AddComponent(new CoreMetadataComponent(1, 2, "core"));

        var found = 0;

        foreach (var _ in Entities.Query<CoreArea>())
            found++;

        Assert.Equal(1, found);
    }

    /// An archetype plus one mod's mixin, which is what a mod actually writes.
    [Fact]
    public void An_archetype_and_an_extension_can_be_queried_together()
    {
        Entities.Create<CoreArea>();

        var visited = 0;

        foreach (var (area, weather) in Entities.Query<CoreArea, Weather>())
        {
            weather.Storm = 5;
            area.Owner = 6;
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void Registering_the_same_extension_twice_adds_it_once()
    {
        var before = ArchetypeRegistrySize();

        WeatherExtends.Register();
        WeatherExtends.Register();

        Assert.Equal(before, ArchetypeRegistrySize());
    }

    private static int ArchetypeRegistrySize()
    {
        var area = new CoreArea();
        return ArchetypeRegistry.SetFor(typeof(CoreArea), ((IArchetypeQueryable)area).Components).Count;
    }

    // -- writing an extended member ----------------------------------------------------------------

    /// Outside a query the shape is an ordinary local, so the property setter is usable.
    [Fact]
    public void An_extended_member_can_be_assigned_through_its_property()
    {
        var area = Entities.Create<CoreArea>();

        area.Temperature = 21;

        Assert.Equal(21, area.Temperature);
    }

    /// Inside a query the shape comes from the loop, which C# will not assign through, so the same
    /// write goes through the method. Both reach the same value.
    [Fact]
    public void An_extended_member_can_be_assigned_through_its_method()
    {
        var area = Entities.Create<CoreArea>();

        area.SetTemperature(7);

        Assert.Equal(7, area.Temperature);

        foreach (var found in Entities.Query<CoreArea>())
            found.SetTemperature(found.Temperature + 1);

        Assert.Equal(8, area.Temperature);
    }

    /// The prefix an extension was declared with names both of them.
    [Fact]
    public void A_prefixed_extension_carries_both_ways_of_writing_it()
    {
        var area = Entities.Create<CoreArea>();

        area.AmbientTemperature = 4;

        Assert.Equal(4, area.AmbientTemperature);

        area.SetAmbientTemperature(5);

        Assert.Equal(5, area.AmbientTemperature);

        // The unprefixed one belongs to a different mixin and is untouched by either write.
        Assert.Equal(0, area.Temperature);
    }

    /// A member declared without a setter gains neither, so nothing can write it by accident.
    [Fact]
    public void A_get_only_extension_has_no_way_to_write_it()
    {
        var area = Entities.Create<CoreArea>();

        // Named rather than fetched: each member is declared twice, once for the shape and once for
        // its chunk view, so asking for one by name is ambiguous.
        var members = typeof(ClimateOnCoreArea).GetMethods().Select(method => method.Name).ToHashSet();

        Assert.Contains("get_Rainfall", members);
        Assert.DoesNotContain("set_Rainfall", members);
        Assert.DoesNotContain("SetRainfall", members);

        Assert.Contains("set_Temperature", members);
        Assert.Contains("SetTemperature", members);

        Assert.Equal(0, area.Rainfall);
    }

    // -- collections carried onto the extended archetype -------------------------------------------

    /// A mixin whose explicit component holds a native collection puts that collection's methods on
    /// the archetype it extends, the same as it puts its fields there.
    [Fact]
    public void A_collection_on_an_extension_reads_and_writes_through_the_archetype()
    {
        var area = Entities.Create<CoreArea>();

        area.AddStarted(7);
        area.AddStarted(9);

        Assert.Equal(2, area.StartedCount);
        Assert.Equal(7, area.GetStarted(0));
        Assert.True(area.ContainsStarted(9));
        Assert.False(area.ContainsStarted(8));
        Assert.Equal([7, 9], area.GetStartedSpan().ToArray());

        area.ClearStarted();

        Assert.Equal(0, area.StartedCount);
    }

    /// The shape a mod actually writes. These are methods, not property setters, so unlike an
    /// assignment they are usable on the loop variable.
    [Fact]
    public void A_collection_on_an_extension_can_be_used_inside_a_query()
    {
        Entities.Create<CoreArea>().Owner = 1;
        Entities.Create<CoreArea>().Owner = 2;

        foreach (var area in Entities.Query<CoreArea>())
            area.AddStarted(area.Owner);

        var seen = 0;

        foreach (var area in Entities.Query<CoreArea>())
        {
            Assert.Equal(1, area.StartedCount);
            Assert.True(area.ContainsStarted(area.Owner));

            seen++;
        }

        Assert.Equal(2, seen);
    }
}
