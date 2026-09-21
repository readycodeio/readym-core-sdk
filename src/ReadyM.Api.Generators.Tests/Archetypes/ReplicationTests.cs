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
}
