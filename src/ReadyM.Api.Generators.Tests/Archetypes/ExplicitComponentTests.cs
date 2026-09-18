using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// What [ExplicitComponent] refuses, run against the real SDK.
public class ExplicitComponentTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Server.Entity.IEntities).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly
    ];

    private const string Core =
        """
        using Friflo.Engine.ECS;

        namespace Core;

        public struct MetaComponent : IComponent
        {
            public readonly int NetId;
            public int Owner;

            public MetaComponent(int netId, int owner)
            {
                NetId = netId;
                Owner = owner;
            }
        }

        public struct LinkedComponent : ILinkComponent
        {
            public Entity Target;

            public Entity GetIndexedValue() => Target;
        }

        public struct SequencesComponent : IComponent
        {
            private System.Collections.Generic.List<int> _started;

            public int StartedCount => _started is null ? 0 : _started.Count;

            public void AddStarted(in int value) => (_started ??= new()).Add(value);

            public void Started_SetFromApi(System.Collections.Generic.List<int> value, int id) => _started = value;
        }

        public struct NotAComponent
        {
            public int Value;
        }
        """;

    private Diagnostic[] Report(string shape)
    {
        var source =
            $$"""
              using ReadyM.SDK.Attributes;

              namespace Mod;

              {{shape}}
              """;

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Core.cs", Core), ("Shape.cs", source)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk);

        return [.. result.DriverRunResult.Diagnostics.Where(d => d.Id.StartsWith("READYM"))];
    }

    private void AssertReports(string id, string shape)
    {
        var reported = Report(shape);

        Assert.Contains(reported, diagnostic => diagnostic.Id == id);
        Assert.All(reported.Where(d => d.Id == id), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

    [Fact]
    public void A_type_that_is_not_a_component_is_refused()
        => AssertReports("READYM004", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.NotAComponent))]
            public readonly partial struct Meta
            {
                public partial int Value { get; set; }
            }
            """);

    /// Link components carry a relationship an accessor cannot maintain, so they are out for now.
    [Fact]
    public void A_link_component_is_refused()
        => AssertReports("READYM004", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.LinkedComponent))]
            public readonly partial struct Linked
            {
                public partial int Target { get; set; }
            }
            """);

    [Fact]
    public void A_property_with_no_matching_member_is_refused()
        => AssertReports("READYM005", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MetaComponent))]
            public readonly partial struct Meta
            {
                public partial int Missing { get; set; }
            }
            """);

    [Fact]
    public void A_setter_over_a_readonly_member_is_refused()
        => AssertReports("READYM006", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MetaComponent))]
            public readonly partial struct Meta
            {
                public partial int NetId { get; set; }
            }
            """);

    // -- what has to be accepted -------------------------------------------------------------------

    [Fact]
    public void Matching_members_are_accepted()
        => Assert.Empty(Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MetaComponent))]
            public readonly partial struct Meta
            {
                public partial int NetId { get; }
                public partial int Owner { get; set; }
            }
            """));

    /// Names are matched ignoring case, which is what makes a camelCase author fit a PascalCase core.
    [Fact]
    public void A_name_differing_only_in_case_is_accepted()
        => Assert.Empty(Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MetaComponent))]
            public readonly partial struct Meta
            {
                public partial int owner { get; set; }
            }
            """));

    [Fact]
    public void A_member_named_outright_is_accepted()
        => Assert.Empty(Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MetaComponent))]
            public readonly partial struct Meta
            {
                [ExplicitMember("Owner")]
                public partial int Holder { get; set; }
            }
            """));

    // -- READYM008: a collection name that forwards nothing ----------------------------------------

    [Fact]
    public void A_collection_name_matching_no_member_is_refused()
        => AssertReports("READYM008", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.SequencesComponent))]
            [ExplicitCollection("Finished")]
            public readonly partial struct Sequences;
            """);

    /// A name that only matches replication plumbing forwards nothing, so it counts as unmatched.
    [Fact]
    public void A_collection_name_matching_only_plumbing_is_refused()
        => AssertReports("READYM008", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.SequencesComponent))]
            [ExplicitCollection("Started_SetFromApi")]
            public readonly partial struct Sequences;
            """);

    [Fact]
    public void A_collection_without_an_explicit_component_is_refused()
        => AssertReports("READYM008", """
            [ArchetypeMixin]
            [ExplicitCollection("Started")]
            public readonly partial struct Sequences;
            """);

    [Fact]
    public void A_collection_name_that_matches_is_accepted()
        => Assert.Empty(Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.SequencesComponent))]
            [ExplicitCollection("Started")]
            public readonly partial struct Sequences;
            """));

    /// The report belongs on the attribute that named it, not on the struct.
    [Fact]
    public void The_report_lands_on_the_attribute()
    {
        var reported = Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.SequencesComponent))]
            [ExplicitCollection("Finished")]
            public readonly partial struct Sequences;
            """);

        var at = Assert.Single(reported).Location;

        Assert.Contains("ExplicitCollection", at.SourceTree!.GetText().ToString(at.SourceSpan));
    }
}
