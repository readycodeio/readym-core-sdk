using System.Reflection;
using ReadyM.Api.Generators.Archetypes;
using ReadyM.Api.Generators.Services;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// <summary>
/// The one entry point a loader calls. A module initializer covers a target that has them, and a
/// netstandard2.0 mod has none at all, so anything left out of here never registers on the client.
/// </summary>
public class RegistrationAggregatorTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(SDK.Attributes.ServiceAttribute).Assembly];

    private string Generated(string source)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Mod.cs", source)],
            [new RegistrationAggregatorGenerator()],
            output,
            Sdk);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    private const string Source = """
        using ReadyM.SDK.Attributes;
        using ReadyM.SDK.Services;

        namespace Mod;

        [ArchetypeMixin]
        public readonly partial struct Stamped
        {
            public partial int Mark { get; set; }

            [CreateHandler]
            private void OnCreated() => Mark = 7;
        }

        [Archetype]
        [Include(typeof(Stamped))]
        public readonly partial struct Parcel;

        [Service]
        public sealed partial class Bookkeeping
        {
            private void Update() { }

            [CreateHandler(typeof(Parcel))]
            private void Track(Parcel parcel) { }
        }

        [ModEntry]
        public sealed partial class Entry
        {
            private void Start() { }
        }

        [ModConfig]
        public sealed partial class Settings
        {
            public int Rounds { get; set; }
        }
        """;

    [Fact]
    public void It_names_every_service()
        => Assert.Contains("global::Mod.Bookkeeping.Registration.Register();", Generated(Source));

    /// A shape's own handler registers from inside the shape, which is just as easy to miss.
    [Fact]
    public void It_names_what_a_shape_handles_for_itself()
        => Assert.Contains("global::Mod.Stamped.OnCreatedRegistration.Register();", Generated(Source));

    /// <summary>
    /// The entry point most of all: it is what builds the mod, so leaving it out starts nothing.
    /// </summary>
    /// <remarks>
    /// Its module initializer only fires once something in the assembly is touched, and the entry
    /// point is the first thing that would touch it, so nothing else would ever run it.
    /// </remarks>
    [Fact]
    public void It_names_the_mods_entry_point()
        => Assert.Contains("global::Mod.Entry.Registration.Register();", Generated(Source));

    /// A config is read before anything in the assembly is touched, so its own initializer is too late.
    [Fact]
    public void It_names_a_mods_config()
        => Assert.Contains("global::Mod.Settings.Registration.Register();", Generated(Source));

    /// Nothing is written for a config the analyzer refuses, so naming it would not compile.
    [Fact]
    public void It_leaves_out_a_config_that_was_refused()
        => Assert.DoesNotContain("Unbuildable", Generated("""
            using ReadyM.SDK.Attributes;

            namespace Mod;

            [Archetype]
            public readonly partial struct Subject
            {
                public partial int Mark { get; set; }
            }

            [ModConfig]
            public abstract partial class Unbuildable { }
            """));

    /// Nothing is written for an entry point the analyzer refuses, so naming it would not compile.
    [Fact]
    public void It_leaves_out_an_entry_point_that_was_refused()
        => Assert.DoesNotContain("Unsealed", Generated("""
            using ReadyM.SDK.Attributes;

            namespace Mod;

            [Archetype]
            public readonly partial struct Subject
            {
                public partial int Mark { get; set; }
            }

            [ModEntry]
            public partial class Unsealed
            {
                private void Start() { }
            }
            """));

    /// Nothing is written for a service the analyzer refuses, so naming it would not compile.
    [Fact]
    public void It_leaves_out_a_service_that_was_refused()
        => Assert.DoesNotContain("Broken", Generated("""
            using ReadyM.SDK.Attributes;
            using ReadyM.SDK.Services;

            namespace Mod;

            [Archetype]
            public readonly partial struct Subject
            {
                public partial int Mark { get; set; }
            }

            [Service]
            public partial class Broken
            {
                private void Update() { }
            }
            """));
}
