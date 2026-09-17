using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Entity;
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
}
