using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// What a shape says about replicating, and what the generator refuses, run against the real SDK.
public class ReplicationTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Server.Entities.IEntities).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly,
        typeof(Api.Multiplayer.ECS.Components.INetworkedComponent).Assembly
    ];

    /// A component that carries values but knows nothing about a wire.
    private const string Core =
        """
        using Friflo.Engine.ECS;

        namespace Core;

        public struct LocalComponent : IComponent
        {
            public int Value;
        }
        """;

    private const string Networked = "global::ReadyM.Api.Multiplayer.ECS.Components.AreaScopeComponent";

    private static string Source(string shape) =>
        $$"""
          using ReadyM.SDK.Attributes;

          namespace Mod;

          {{shape}}
          """;

    private Diagnostic[] Report(string shape)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Core.cs", Core), ("Shape.cs", Source(shape))],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);

        return [.. result.DriverRunResult.Diagnostics.Where(d => d.Id.StartsWith("READYM"))];
    }

    /// Everything the run emitted, as one string to read assertions off.
    private string Generated(string shape)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Core.cs", Core), ("Shape.cs", Source(shape))],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    private const string ReplicatedShape =
        """
        [ArchetypeMixin]
        [Replicated]
        public readonly partial struct Vitals
        {
            public partial int Health { get; set; }
        }
        """;

    // -- what a write through a replicated shape means ----------------------------------------------

    /// The plain setter reports what the game did, so the game keeps authority over the value.
    [Fact]
    public void A_replicated_setter_writes_as_a_mirror()
        => Assert.Contains(
            "Fields.Health, value, global::ReadyM.SDK.Entities.WriteKind.Mirror",
            Generated(ReplicatedShape));

    /// Overriding goes through the value's token and an extension only a client references, so
    /// nothing here emits it and a server mod never sees a verb it cannot use.
    [Fact]
    public void No_override_verb_is_generated()
    {
        var generated = Generated(ReplicatedShape);

        Assert.DoesNotContain("OverrideHealth", generated);
        Assert.DoesNotContain("WriteKind.Override", generated);
    }

    /// Nothing carries the value over a wire, so there is no mask to set and nothing to override.
    [Fact]
    public void An_unreplicated_setter_is_written_outright()
    {
        var generated = Generated(
            """
            [ArchetypeMixin]
            public readonly partial struct Vitals
            {
                public partial int Health { get; set; }
            }
            """);

        Assert.DoesNotContain("WriteKind", generated);
    }

    // -- naming a value without naming the component ------------------------------------------------

    /// An assembly that cannot see the component still has to say which value it means.
    [Fact]
    public void A_replicated_shape_names_its_values()
    {
        var generated = Generated(ReplicatedShape);

        Assert.Contains("public static class Field", generated);
        Assert.Contains("Health { get; }", generated);
    }

    /// The token carries the shape, so a value of one cannot be passed where another is wanted.
    [Fact]
    public void A_token_is_typed_to_its_shape()
        => Assert.Contains(
            "global::ReadyM.SDK.Archetypes.Access.Value<global::Mod.Vitals, int> Health",
            Generated(ReplicatedShape));

    /// Nothing carries it over a wire, so there is no field table to build a token from.
    [Fact]
    public void An_unreplicated_shape_names_none()
        => Assert.DoesNotContain("public static class Field", Generated(
            """
            [ArchetypeMixin]
            public readonly partial struct Vitals
            {
                public partial int Health { get; set; }
            }
            """));

    /// A shape that declares itself a scope converts to one, which is what lets a query read
    /// InScope(area) rather than naming the handle. Nothing else in the generator checks that the
    /// marker is still matched the way a symbol spells it.
    [Fact]
    public void A_scope_archetype_converts_to_one()
        => Assert.Contains(
            "public static implicit operator global::ReadyM.SDK.Archetypes.Scope",
            Generated(
                """
                [Archetype]
                public readonly partial struct Camp : global::ReadyM.SDK.Archetypes.IScope
                {
                    public partial int Size { get; set; }
                }
                """));

    private DeclarationModel Model(string shape)
    {
        var compilation = SourceGeneratorTestHelper.CreateCompilation(
            [("Core.cs", Core), ("Shape.cs", Source(shape))], output, Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);

        var symbol = compilation.GetTypeByMetadataName("Mod.Subject");

        Assert.NotNull(symbol);

        return DeclarationModel.For(symbol);
    }

    // -- what the generator refuses ----------------------------------------------------------------

    /// A shape with no values of its own has nothing to send. Its presence travels with the entity.
    [Fact]
    public void A_marker_shape_cannot_replicate()
    {
        var reported = Report("""
            [Archetype]
            [Replicated]
            public readonly partial struct Subject;
            """);

        Assert.Contains(reported, d => d.Id == "READYM009");
        Assert.All(reported.Where(d => d.Id == "READYM009"), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

    /// Storage that already exists cannot be taught to replicate, so asking is an error rather than
    /// something quietly ignored.
    [Fact]
    public void A_shape_over_a_local_component_cannot_replicate()
    {
        var reported = Report("""
            [ArchetypeMixin]
            [Replicated]
            [ExplicitComponent(typeof(global::Core.LocalComponent))]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.Contains(reported, d => d.Id == "READYM010");
    }

    [Fact]
    public void A_shape_with_values_of_its_own_replicates_without_complaint()
    {
        var reported = Report("""
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id is "READYM009" or "READYM010");
    }

    [Fact]
    public void A_shape_over_a_networked_component_replicates_without_complaint()
    {
        var reported = Report($$"""
            [ArchetypeMixin]
            [Replicated]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Subject;
            """);

        Assert.DoesNotContain(reported, d => d.Id is "READYM009" or "READYM010");
    }

    // -- what the model decides --------------------------------------------------------------------

    [Fact]
    public void A_shape_that_did_not_ask_does_not_replicate()
    {
        var model = Model("""
            [ArchetypeMixin]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.False(model.HasReplicatedAttribute);
        Assert.False(model.IsReplicated);
    }

    /// Storage that is already networked says so itself, so the shape over it inherits that.
    [Fact]
    public void A_networked_component_replicates_without_the_attribute()
    {
        var model = Model($$"""
            [ArchetypeMixin]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Subject;
            """);

        Assert.False(model.HasReplicatedAttribute);
        Assert.True(model.IsReplicated);
    }

    /// A marker replicates nothing whatever it asks for, which is what the diagnostic says too.
    [Fact]
    public void A_marker_shape_does_not_replicate_even_when_it_asks()
    {
        var model = Model("""
            [Archetype]
            [Replicated]
            public readonly partial struct Subject;
            """);

        Assert.True(model.HasReplicatedAttribute);
        Assert.False(model.IsReplicated);
    }

    [Fact]
    public void Delivery_is_reliable_unless_the_shape_says_otherwise()
    {
        var model = Model("""
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.True(model.IsReplicated);
        Assert.Equal(Delivery.Reliable, model.Delivery);
    }

    [Fact]
    public void Delivery_is_carried_from_the_attribute()
    {
        var model = Model("""
            [ArchetypeMixin]
            [Replicated(Delivery.Unreliable)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.Equal(Delivery.Unreliable, model.Delivery);
    }

    // -- collections -------------------------------------------------------------------------------

    /// Handing a caller the collection itself lets them change it without the change being sent,
    /// so a replicated shape has to keep it private and let the generated members carry it.
    [Fact]
    public void A_replicated_collection_that_is_reachable_as_a_whole_is_refused()
    {
        var reported = Report("""
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Subject
            {
                public partial global::Yooni.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

        Assert.Contains(reported, d => d.Id == "READYM011");
    }

    [Fact]
    public void A_private_replicated_collection_is_accepted()
    {
        var reported = Report("""
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Subject
            {
                private partial global::Yooni.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id == "READYM011");
    }

    /// A shape that does not replicate has no generated members to reach a collection through, so
    /// keeping it private would leave it unreachable.
    [Fact]
    public void A_local_collection_may_be_reachable_as_a_whole()
    {
        var reported = Report("""
            [ArchetypeMixin]
            public readonly partial struct Subject
            {
                public partial global::Yooni.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id == "READYM011");
    }
}
