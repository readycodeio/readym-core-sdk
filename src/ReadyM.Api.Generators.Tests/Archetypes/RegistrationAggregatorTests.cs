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
        """;

    [Fact]
    public void It_names_every_service()
        => Assert.Contains("global::Mod.Bookkeeping.Registration.Register();", Generated(Source));

    /// A shape's own handler registers from inside the shape, which is just as easy to miss.
    [Fact]
    public void It_names_what_a_shape_handles_for_itself()
        => Assert.Contains("global::Mod.Stamped.OnCreatedRegistration.Register();", Generated(Source));

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
