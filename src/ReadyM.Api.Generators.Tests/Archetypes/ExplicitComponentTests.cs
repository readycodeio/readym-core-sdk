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
        typeof(SDK.Server.Entities.IEntities).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly
    ];

    private const string Core =
        """
        using Friflo.Engine.ECS;

        [assembly: GlobalGenericInstanceType(typeof(global::Core.HoldsComponent<>), "holds-actor", typeof(global::Core.Actor))]

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

        /// The same surface, on a component that crosses the wire, which is what makes a change to
        /// its collection a write the policy decides on.
        public struct TrackedComponent : global::ReadyM.Api.Multiplayer.ECS.Components.INetworkedComponent
        {
            public int StartedCount => 0;

            public int GetStarted(int index) => 0;

            public void AddStarted(in int value) { }

            public void ClearStarted() { }
        }

        public struct NotAComponent
        {
            public int Value;
        }

        public class Actor;

        public class Character : Actor;

        public class Prop;

        public struct MappedComponent : IComponent
        {
            public Actor Held;
        }

        public struct HoldsComponent<T> : IComponent
        {
            public T Value;
        }

        [GenericInstanceType("named-prop", typeof(Prop))]
        public struct NamesComponent<T> : IComponent
        {
            public T Value;
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
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);

        return [.. result.DriverRunResult.Diagnostics.Where(d => d.Id.StartsWith("READYM"))];
    }

    /// Everything the run emitted, as one string to read assertions off.
    private static int Occurrences(string text, string looked)
    {
        var found = 0;

        for (var at = text.IndexOf(looked, StringComparison.Ordinal); at >= 0;
             at = text.IndexOf(looked, at + looked.Length, StringComparison.Ordinal))
            found++;

        return found;
    }

    private string Generated(string shape)
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
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
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

    // -- READYM012: a member the shape cannot read -------------------------------------------------

    /// One component holds what several kinds of entity map to, so a shape says which kind it is
    /// about and reads the value as that.
    [Fact]
    public void A_derived_type_is_accepted()
        => Assert.Empty(Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MappedComponent))]
            public readonly partial struct Mapped
            {
                public partial global::Core.Character? Held { get; }
            }
            """));

    [Fact]
    public void An_unrelated_type_is_refused()
        => AssertReports("READYM012", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MappedComponent))]
            public readonly partial struct Mapped
            {
                public partial global::Core.Prop? Held { get; }
            }
            """);

    /// A value of the wrong kind reads as null rather than throwing, which is what a shape over
    /// storage shared by several kinds of entity needs.
    [Fact]
    public void A_derived_type_is_read_with_a_cast()
    {
        var generated = Generated("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MappedComponent))]
            public readonly partial struct Mapped
            {
                public partial global::Core.Character? Held { get; }
            }
            """);

        Assert.Contains("global::Core.MappedComponent>().Held as global::Core.Character", generated);
    }

    /// The same value declared as what the component holds is read straight through.
    [Fact]
    public void A_matching_type_is_read_without_a_cast()
    {
        var generated = Generated("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.MappedComponent))]
            public readonly partial struct Mapped
            {
                public partial global::Core.Actor? Held { get; }
            }
            """);

        Assert.DoesNotContain(" as global::Core.Actor", generated);
    }

    // -- READYM013: a generic component the schema does not hold -----------------------------------

    [Fact]
    public void An_unregistered_instantiation_is_refused()
        => AssertReports("READYM013", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.HoldsComponent<global::Core.Prop>))]
            public readonly partial struct Holds
            {
                public partial global::Core.Prop? Value { get; }
            }
            """);

    [Fact]
    public void An_instantiation_an_assembly_registered_is_accepted()
        => Assert.Empty(Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.HoldsComponent<global::Core.Actor>))]
            public readonly partial struct Holds
            {
                public partial global::Core.Actor? Value { get; }
            }
            """));

    /// The generic type can name its own instantiations, which is what a component declared
    /// alongside the ECS does.
    [Fact]
    public void An_instantiation_the_component_registered_is_accepted()
        => Assert.Empty(Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.NamesComponent<global::Core.Prop>))]
            public readonly partial struct Names
            {
                public partial global::Core.Prop? Value { get; }
            }
            """));

    /// The report says what to add, because the fix is one attribute and nothing points to it.
    [Fact]
    public void The_report_names_the_attribute_to_add()
    {
        var reported = Report("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.HoldsComponent<global::Core.Prop>))]
            public readonly partial struct Holds
            {
                public partial global::Core.Prop? Value { get; }
            }
            """);

        var message = Assert.Single(reported, d => d.Id == "READYM013").GetMessage();

        Assert.Contains("GlobalGenericInstanceType(typeof(global::Core.HoldsComponent)", message);
        Assert.Contains("typeof(Core.Prop)", message);
    }

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

    /// A collection on a component that replicates is changed under the same rule a value is, and
    /// says so where the analyzer can read it. The component being borrowed changes nothing here.
    [Fact]
    public void A_borrowed_collection_that_replicates_is_changed_under_the_policy()
    {
        var generated = Generated("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.TrackedComponent))]
            [ExplicitCollection("Started")]
            public readonly partial struct Sequences;
            """);

        Assert.Contains("if (!handle.Mirrors(global::Core.TrackedComponent.Fields.Started))", generated);
        Assert.Contains("ChangesAttribute(\"Sequences.Field.Started\")", generated);
    }

    /// Reading it is not a write, so only the members that change it ask anything: Add and Clear,
    /// and not Count or Get.
    [Fact]
    public void A_borrowed_collection_is_read_without_asking()
    {
        var generated = Generated("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.TrackedComponent))]
            [ExplicitCollection("Started")]
            public readonly partial struct Sequences;
            """);

        Assert.Equal(2, Occurrences(generated, "handle.Mirrors("));
    }

    /// A component nothing sends has no Fields table to ask, and nothing to keep a caller from.
    [Fact]
    public void A_borrowed_collection_that_does_not_replicate_is_left_alone()
    {
        var generated = Generated("""
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.SequencesComponent))]
            [ExplicitCollection("Started")]
            public readonly partial struct Sequences;
            """);

        Assert.DoesNotContain("Mirrors(", generated);
        Assert.DoesNotContain("ChangesAttribute", generated);
    }

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
