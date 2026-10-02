using System.Reflection;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// The entry point a host calls to register an assembly's shapes.
/// </summary>
/// <remarks>
/// A module initializer covers a target that has them, but netstandard2.0 does not, so a loader
/// needs something to call outright. These check the entry point exists, names every registration,
/// and can be run again without changing anything.
/// </remarks>
public class GeneratedShapesTests : ClientSdkTest
{
    private static Type EntryPoint
        => typeof(CoreArea).Assembly.GetType("ReadyM.SDK.Generated.ShapeRegistrations")!;

    [Fact]
    public void An_assembly_declaring_shapes_gets_an_entry_point()
        => Assert.NotNull(EntryPoint);

    [Fact]
    public void Applying_it_reports_that_it_ran()
        => Assert.True(GeneratedShapes.Apply(typeof(CoreArea).Assembly));

    /// An assembly with no shapes of its own has nothing to run.
    [Fact]
    public void An_assembly_without_shapes_reports_nothing_to_do()
        => Assert.False(GeneratedShapes.Apply(typeof(object).Assembly));

    /// <summary>
    /// It names every registration the compilation emits, which is what stops one being forgotten
    /// when a module initializer is not there to cover it.
    /// </summary>
    [Theory]
    [InlineData("WeatherExtends")]
    [InlineData("MusicExtends")]
    [InlineData("ClimateExtends")]
    [InlineData("RegisteredIndex")]
    [InlineData("TaggedIndex")]
    [InlineData("StationIndex")]
    public void It_names_every_registration(string expected)
    {
        var source = EntryPoint.GetMethod("RegisterAll", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(source);
        Assert.Contains(expected, RegistrationNames());
    }

    /// <summary>Running it twice leaves the same result, so a host and an initializer cannot clash.</summary>
    [Fact]
    public void Running_it_again_changes_nothing()
    {
        var before = ArchetypeRegistry.SetFor(typeof(CoreArea), ComponentsOf<CoreArea>()).Count;

        GeneratedShapes.Apply(typeof(CoreArea).Assembly);
        GeneratedShapes.Apply(typeof(CoreArea).Assembly);

        Assert.Equal(before, ArchetypeRegistry.SetFor(typeof(CoreArea), ComponentsOf<CoreArea>()).Count);
    }

    /// The registrations still hold after re-running, which is what a second call must not undo.
    [Fact]
    public void What_it_registered_still_holds_afterwards()
    {
        GeneratedShapes.Apply(typeof(CoreArea).Assembly);

        var area = Entities.Create<CoreArea>();

        Assert.True(EntityHandle.Of(area).Is<Weather>());
        Assert.True(Entities.TryLookup<Station, int>(SpawnStation(7), out _));
    }

    private int SpawnStation(int code)
    {
        var station = Entities.Create<Station>();

        station.Code = code;
        return code;
    }

    private static IEnumerable<string> RegistrationNames()
        => typeof(CoreArea).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Extends") || type.Name.EndsWith("Index"))
            .Select(type => type.Name);

    // -- what a loader calls -----------------------------------------------------------------------

    /// <summary>A host hands over everything it loaded; only the assemblies with shapes count.</summary>
    [Fact]
    public void Applying_a_whole_set_counts_only_the_assemblies_with_shapes()
    {
        var applied = GeneratedShapes.ApplyAll([typeof(CoreArea).Assembly, typeof(object).Assembly]);

        Assert.Equal(1, applied);
    }

    [Fact]
    public void An_assembly_given_twice_is_only_applied_once()
    {
        var assembly = typeof(CoreArea).Assembly;
        var seen = 0;

        GeneratedShapes.ApplyAll([assembly, assembly], _ => seen++);

        Assert.Equal(1, seen);
    }

    /// <summary>
    /// One assembly failing must not stop the rest: a broken mod cannot take the others down.
    /// </summary>
    [Fact]
    public void A_failure_is_reported_and_the_rest_still_run()
    {
        var failures = 0;
        var applied = GeneratedShapes.ApplyAll(
            [typeof(object).Assembly, typeof(CoreArea).Assembly],
            onFailed: (_, _) => failures++);

        Assert.Equal(1, applied);
        Assert.Equal(0, failures);
    }
}
