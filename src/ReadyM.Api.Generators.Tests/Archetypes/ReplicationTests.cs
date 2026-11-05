using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// <summary>What a shape says about replicating, and what the generator refuses, run against the real SDK.</summary>
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

    /// <summary>A component that carries values but knows nothing about a wire.</summary>
    private const string Core =
        """
        using Friflo.Engine.ECS;

        namespace Core;

        public struct LocalComponent : IComponent
        {
            public int Value;
        }

        public struct OwnedComponent : IComponent, ReadyM.Api.Mapping.Tags.IOwnershipBased
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

    private void AssertReports(string id, string shape)
    {
        var reported = Report(shape);

        Assert.Contains(reported, diagnostic => diagnostic.Id == id);
        Assert.All(reported.Where(d => d.Id == id), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

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

    /// <summary>Everything the run emitted, as one string to read assertions off.</summary>
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
        [ArchetypeMixin(Replication.OwnershipBased)]
        public readonly partial struct Vitals
        {
            public partial int Health { get; set; }
        }
        """;

    // -- what a write through a replicated shape means ----------------------------------------------

    /// <summary>The plain setter reports what the game did, so the game keeps authority over the value.</summary>
    [Fact]
    public void A_replicated_setter_writes_as_a_mirror()
        => Assert.Contains(
            "Fields.Health, value, global::ReadyM.SDK.Entities.WriteKind.Mirror",
            Generated(ReplicatedShape));

    /// <summary>
    /// Overriding goes through the value's token and an extension only a client references, so
    /// nothing here emits it and a server mod never sees a verb it cannot use.
    /// </summary>
    [Fact]
    public void No_override_verb_is_generated()
    {
        var generated = Generated(ReplicatedShape);

        Assert.DoesNotContain("OverrideHealth", generated);
        Assert.DoesNotContain("WriteKind.Override", generated);
    }

    /// <summary>Nothing carries the value over a wire, so there is no mask to set and nothing to override.</summary>
    [Fact]
    public void An_unreplicated_setter_is_written_outright()
    {
        var generated = Generated(
            """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Vitals
            {
                public partial int Health { get; set; }
            }
            """);

        Assert.DoesNotContain("WriteKind", generated);
    }

    // -- naming a value without naming the component ------------------------------------------------

    /// <summary>An assembly that cannot see the component still has to say which value it means.</summary>
    [Fact]
    public void A_replicated_shape_names_its_values()
    {
        var generated = Generated(ReplicatedShape);

        Assert.Contains("public static class Field", generated);
        Assert.Contains("Health { get; }", generated);
    }

    /// <summary>The token carries the shape, so a value of one cannot be passed where another is wanted.</summary>
    [Fact]
    public void A_token_is_typed_to_its_shape()
        => Assert.Contains(
            "global::ReadyM.SDK.Archetypes.Access.Value<global::Mod.Vitals, int> Health",
            Generated(ReplicatedShape));

    /// <summary>Nothing carries it over a wire, so there is no field table to build a token from.</summary>
    [Fact]
    public void An_unreplicated_shape_names_none()
        => Assert.DoesNotContain("public static class Field", Generated(
            """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Vitals
            {
                public partial int Health { get; set; }
            }
            """));

    /// <summary>
    /// A shape that declares itself a scope converts to one, which is what lets a query read
    /// InScope(area) rather than naming the handle. Nothing else in the generator checks that the
    /// marker is still matched the way a symbol spells it.
    /// </summary>
    [Fact]
    public void A_scope_archetype_converts_to_one()
        => Assert.Contains(
            "public static implicit operator global::ReadyM.SDK.Archetypes.Scope",
            Generated(
                """
                [Archetype(Networked = false)]
                public readonly partial struct Camp : global::ReadyM.SDK.Archetypes.IScope
                {
                    public partial int Size { get; set; }
                }
                """));

    // -- what the generated component compiles to ------------------------------------------------

    /// <summary>
    /// The values and the masks sit in two declarations of one struct. Left at the default layout, the
    /// compiler warns that their order is undefined (CS0282), and every mod with a replicated shape saw it.
    /// </summary>
    [Theory]
    [InlineData(ReplicatedShape)]
    [InlineData("""
        [Archetype]
        public readonly partial struct Crate
        {
            public partial int Size { get; set; }
        }
        """)]
    public void A_replicated_component_compiles_without_a_layout_warning(string shape)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Core.cs", Core), ("Shape.cs", Source(shape))],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);

        Assert.DoesNotContain(result.OutputDiagnostics, d => d.Id == "CS0282");
        Assert.DoesNotContain(result.OutputDiagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    // -- what a client build leaves out ---------------------------------------------------------

    private string GeneratedFor(string shape, string side)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Core.cs", Core), ("Shape.cs", Source(shape))],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly,
            new Dictionary<string, string> { ["build_property.SdkRuntime"] = side });

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    /// <summary>A chunk setter takes the row's handle, which is what its write stamps the server's time with.</summary>
    private const string ChunkSetter = "chunk, int index, in global::ReadyM.SDK.Entities.EntityHandle handle, int value";

    /// <summary>
    /// A chunk write goes straight into the component array, past the policy and the masks that a
    /// client write has to obey, so a client build does not get one.
    /// </summary>
    [Fact]
    public void A_client_build_has_no_chunk_setter()
    {
        var generated = GeneratedFor(ReplicatedShape, "Client");

        Assert.DoesNotContain(ChunkSetter, generated);
        Assert.Contains("chunk, int index)", generated);
    }

    [Fact]
    public void A_server_build_keeps_its_chunk_setters()
        => Assert.Contains(ChunkSetter, GeneratedFor(ReplicatedShape, "Server"));

    /// <summary>A build that says nothing is a server, so nothing an existing project has is taken away.</summary>
    [Fact]
    public void An_unstated_side_keeps_them()
        => Assert.Contains(ChunkSetter, Generated(ReplicatedShape));

    /// <summary>
    /// A replicated value is written through the row's handle, the same write a handle's setter makes, so a
    /// write in a query loop stamps the field just as one through a handle does.
    /// </summary>
    [Fact]
    public void A_chunk_setter_writes_a_replicated_value_through_the_handle()
        => Assert.Contains("=> handle.Write(ref chunk.As<", GeneratedFor(ReplicatedShape, "Server"));

    private const string CarryingShape =
        """
        [ArchetypeMixin(Replication.OwnershipBased)]
        public readonly partial struct Vitals
        {
            public partial int Health { get; set; }
        }

        [Archetype(Networked = false)]
        [Include(typeof(Vitals))]
        public readonly partial struct Fighter;
        """;

    /// <summary>
    /// A value reaching an archetype through [Include] is generated outright, so a client build can
    /// leave the write off it and send a mod to the token instead.
    /// </summary>
    private const string Writes = "set => global::Mod.VitalsAccessors.SetHealth(_handle, value);";

    /// <summary>
    /// Both shapes are in one compilation, so the count says which of them kept a write: the mixin
    /// always does, and the archetype including it does only on a server.
    /// </summary>
    private static int WritesIn(string generated)
        => generated.Split([Writes], StringSplitOptions.None).Length - 1;

    [Fact]
    public void A_client_build_cannot_write_an_included_value()
    {
        var generated = GeneratedFor(CarryingShape, "Client");

        Assert.Contains("public int Health => global::Mod.VitalsAccessors.GetHealth(_handle);", generated);
        Assert.Equal(1, WritesIn(generated));
    }

    [Fact]
    public void A_server_build_can()
        => Assert.Equal(2, WritesIn(GeneratedFor(CarryingShape, "Server")));

    /// <summary>
    /// The mixin's own declaration says { get; set; }, and C# requires the implementing part to
    /// implement every accessor it declares, so this one setter cannot be dropped.
    /// </summary>
    [Fact]
    public void The_shape_that_declared_it_keeps_its_own_setter()
        => Assert.Contains("set => global::Mod.VitalsAccessors.SetHealth(_handle, value);",
            GeneratedFor(ReplicatedShape, "Client"));

    // -- how a shape says its values travel ---------------------------------------------------------

    /// <summary>The declaration lives on the shape; the component carries the contract the runtime looks for.</summary>
    [Fact]
    public void A_stated_propagation_reaches_the_component()
        => Assert.Contains(
            "global::ReadyM.Api.Mapping.Tags.IOwnershipBased",
            Generated(
                """
                [ArchetypeMixin(Replication.OwnershipBased)]
                public readonly partial struct Vitals
                {
                    public partial int Health { get; set; }
                }
                """));

    /// <summary>Nothing sends a local shape's values, so it gets plain accessors, no mask and no tokens.</summary>
    [Fact]
    public void A_local_shape_is_left_alone()
    {
        var generated = Generated(
            """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Ambient
            {
                public partial int Temperature { get; set; }
            }
            """);

        Assert.DoesNotContain("ReadyM.Api.Mapping.Tags.I", generated);
        Assert.DoesNotContain("public static class Field", generated);
        Assert.DoesNotContain("WriteDelta", generated);
    }

    /// <summary>A networked archetype's own values are sent only as it says, so it has to say.</summary>
    [Fact]
    public void A_networked_archetype_with_values_must_state_replication()
        => AssertReports("READYM014", """
            [Archetype]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

    [Fact]
    public void Stating_none_keeps_a_networked_archetypes_values_local()
    {
        var reported = Report("""
            [Archetype(Replication = Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id == "READYM014");
        Assert.False(Model("""
            [Archetype(Replication = Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """).IsReplicated);
    }

    /// <summary>Nothing of it is sent, so nobody is kept from writing it and there is nothing to state.</summary>
    [Fact]
    public void An_archetype_that_is_not_networked_must_not_state_replication()
        => AssertReports("READYM016", """
            [Archetype(Networked = false, Replication = Replication.Both)]
            public readonly partial struct Region
            {
                public partial int Temperature { get; set; }
            }
            """);

    /// <summary>It holds nothing of its own, so Replication has nothing to decide on it.</summary>
    [Fact]
    public void An_archetype_that_only_includes_must_not_state_replication()
        => AssertReports("READYM009", """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Ambient
            {
                public partial int Temperature { get; set; }
            }

            [Archetype(Replication = Replication.OwnershipBased)]
            [Include(typeof(Ambient))]
            public readonly partial struct Region;
            """);

    /// <summary>The component behind the shape decides, so the shape has to say what it decides.</summary>
    [Fact]
    public void A_shape_over_a_component_must_state_what_it_does()
    {
        var error = Assert.Single(Report("""
            [ArchetypeMixin(Replication.OwnershipBased)]
            [ExplicitComponent(typeof(global::Core.LocalComponent))]
            public readonly partial struct Meta
            {
                public partial int Value { get; set; }
            }
            """), d => d.Id == "READYM015");

        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("say Replication.None", error.GetMessage());
    }

    /// <summary>A contract on a component nothing sends does not make it replicate.</summary>
    [Fact]
    public void A_local_component_is_none_whatever_contract_it_carries()
        => Assert.DoesNotContain(Report("""
            [ArchetypeMixin(Replication.None)]
            [ExplicitComponent(typeof(global::Core.OwnedComponent))]
            public readonly partial struct Owned
            {
                public partial int Value { get; set; }
            }
            """), d => d.Id == "READYM015");

    /// <summary>The server takes a networked component's changes from its owner unless it says otherwise.</summary>
    [Fact]
    public void A_networked_component_without_a_contract_is_ownership_based()
    {
        Assert.DoesNotContain(Report($$"""
            [ArchetypeMixin(Replication.OwnershipBased)]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Subject;
            """), d => d.Id.StartsWith("READYM"));

        var error = Assert.Single(Report($$"""
            [ArchetypeMixin(Replication.ServerAuthoritative)]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Subject;
            """), d => d.Id == "READYM015");

        Assert.Contains("say Replication.OwnershipBased", error.GetMessage());
    }

    /// <summary>A networked archetype over a component, with values of its own, states it like any other.</summary>
    [Fact]
    public void A_networked_archetype_over_a_component_must_state_it_too()
        => AssertReports("READYM014", $$"""
            [Archetype]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Region
            {
                public partial global::ReadyM.Api.Idents.AreaId AreaId { get; set; }
            }
            """);

    /// <summary>Only a shape sending values of its own has a Delivery to choose.</summary>
    [Theory]
    [InlineData("[ArchetypeMixin(Replication.None, Delivery = Delivery.Unreliable)]\npublic readonly partial struct Subject { public partial int Value { get; set; } }")]
    [InlineData("[Archetype(Delivery = Delivery.Unreliable)]\npublic readonly partial struct Subject;")]
    [InlineData("[Archetype(Networked = false, Delivery = Delivery.Unreliable)]\npublic readonly partial struct Subject { public partial int Value { get; set; } }")]
    public void Delivery_on_a_shape_that_sends_nothing_of_its_own_is_refused(string shape)
        => AssertReports("READYM050", shape);

    [Fact]
    public void Delivery_on_a_shape_that_sends_its_values_is_fine()
        => Assert.DoesNotContain(Report("""
            [ArchetypeMixin(Replication.OwnershipBased, Delivery = Delivery.Unreliable)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """), d => d.Id == "READYM050");


    /// <summary>
    /// The field a value is held in is its name with a lower first letter, which can land on a
    /// keyword. Sealed, Class and Event are ordinary things for a mod to call a value.
    /// </summary>
    [Fact]
    public void A_value_whose_field_would_be_a_keyword_is_escaped()
    {
        var generated = Generated("""
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Sealed { get; set; }
                public partial int Event { get; set; }
                public partial int Ordinary { get; set; }
            }
            """);

        Assert.Contains("public int @sealed;", generated);
        Assert.Contains("public int @event;", generated);
        Assert.Contains("public int ordinary;", generated);
    }

    /// <summary>The same for one that replicates, where the field is reached from more places.</summary>
    [Fact]
    public void And_escaped_on_a_shape_that_replicates()
        => Assert.Contains("@sealed", Generated("""
            [ArchetypeMixin(Replication.OwnershipBased)]
            public readonly partial struct Subject
            {
                public partial int Sealed { get; set; }
            }
            """));

    // -- what a shape asks to run as its entities appear -------------------------------------------

    /// <summary>
    /// A handler hangs on everything the shape is made of, the way a query matches it. Keying it on
    /// one component would leave an archetype that only includes other shapes with nothing to hang on.
    /// </summary>
    [Fact]
    public void A_handler_hangs_on_everything_the_shape_is_made_of()
        => Assert.Contains(
            "CreateHandlerRegistry.Register(global::Mod.SubjectAccessors.Components,",
            Generated("""
                [Archetype(Networked = false)]
                public readonly partial struct Subject
                {
                    [CreateHandler]
                    private void OnCreated() { }
                }
                """));

    /// <summary>What it asks for comes from the game's services, resolved as it runs rather than before.</summary>
    [Fact]
    public void A_create_handler_is_handed_what_it_asked_for()
        => Assert.Contains(
            "Services.Resolve<global::Core.LocalComponent>()",
            Generated("""
                [ArchetypeMixin(Replication.None)]
                public readonly partial struct Subject
                {
                    public partial int Value { get; set; }

                    [CreateHandler]
                    private void OnCreated(global::Core.LocalComponent needed) { }
                }
                """));

    /// <summary>There is nobody to hand a result to, and nothing for a static one to run on.</summary>
    [Fact]
    public void A_create_handler_returning_something_is_refused()
        => AssertReports("READYM020", """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }

                [CreateHandler]
                private int OnCreated() => 1;
            }
            """);

    /// <summary>The order between two would be nobody's to say, so the shape keeps one.</summary>
    [Fact]
    public void A_shape_declaring_two_create_handlers_is_refused()
        => AssertReports("READYM021", """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }

                [CreateHandler]
                private void First() { }

                [CreateHandler]
                private void Second() { }
            }
            """);

    /// <summary>The mirror of a create handler, and its registration sits inside the shape the same way.</summary>
    [Fact]
    public void A_shape_registers_what_it_asked_to_run_as_one_of_its_entities_goes()
        => Assert.Contains(
            "DeleteHandlerRegistry.Register(global::Mod.SubjectAccessors.Components,",
            Generated("""
                [Archetype(Networked = false)]
                public readonly partial struct Subject
                {
                    [DeleteHandler]
                    private void OnDeleted() { }
                }
                """));

    /// <summary>A shape may declare both, and they register separately.</summary>
    [Fact]
    public void A_shape_may_be_told_about_both_ends()
    {
        var generated = Generated("""
            [Archetype(Networked = false)]
            public readonly partial struct Subject
            {
                [CreateHandler]
                private void OnCreated() { }

                [DeleteHandler]
                private void OnDeleted() { }
            }
            """);

        Assert.Contains("class OnCreatedRegistration", generated);
        Assert.Contains("class OnDeletedDeleteRegistration", generated);
    }

    [Fact]
    public void A_delete_handler_returning_something_is_refused()
        => AssertReports("READYM028", """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }

                [DeleteHandler]
                private int OnDeleted() => 1;
            }
            """);

    [Fact]
    public void A_shape_declaring_two_delete_handlers_is_refused()
        => AssertReports("READYM029", """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }

                [DeleteHandler]
                private void First() { }

                [DeleteHandler]
                private void Second() { }
            }
            """);

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

    /// <summary>A shape with no values of its own has nothing to send. Its presence travels with the entity.</summary>
    [Fact]
    public void A_marker_shape_cannot_replicate()
    {
        var reported = Report("""
            [ArchetypeMixin(Replication.OwnershipBased)]
            public readonly partial struct Subject;
            """);

        Assert.Contains(reported, d => d.Id == "READYM009");
        Assert.All(reported.Where(d => d.Id == "READYM009"), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

    /// <summary>A marker states None: there is nothing of it to send, and its presence travels with the entity.</summary>
    [Fact]
    public void A_marker_shape_states_none_without_complaint()
        => Assert.DoesNotContain(Report("""
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject;
            """), d => d.Id.StartsWith("READYM"));

    [Fact]
    public void A_shape_with_values_of_its_own_replicates_without_complaint()
    {
        var reported = Report("""
            [ArchetypeMixin(Replication.OwnershipBased)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id.StartsWith("READYM"));
    }

    // -- what the model decides --------------------------------------------------------------------

    [Fact]
    public void A_shape_stating_none_does_not_replicate()
    {
        var model = Model("""
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.Equal(Replication.None, model.Replication);
        Assert.False(model.IsReplicated);
    }

    /// <summary>A networked component sends itself, so the shape over it replicates as the component does.</summary>
    [Fact]
    public void A_networked_component_replicates_as_it_does()
    {
        var model = Model($$"""
            [ArchetypeMixin(Replication.OwnershipBased)]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Subject;
            """);

        Assert.True(model.IsReplicated);
    }

    /// <summary>
    /// An archetype's entities are networked unless it says otherwise. Its values travel separately: a marker
    /// replicates none, and the runtime reads whether the entity is networked off the archetype.
    /// </summary>
    [Fact]
    public void An_archetype_is_networked_unless_it_says_otherwise()
    {
        var model = Model("""
            [Archetype]
            public readonly partial struct Subject;
            """);

        Assert.True(model.Networked);
        Assert.False(model.IsReplicated);
        Assert.False(Model("""
            [Archetype(Networked = false)]
            public readonly partial struct Subject;
            """).Networked);
    }

    [Fact]
    public void Delivery_is_reliable_unless_the_shape_says_otherwise()
    {
        var model = Model("""
            [ArchetypeMixin(Replication.OwnershipBased)]
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
            [ArchetypeMixin(Replication.OwnershipBased, Delivery = Delivery.Unreliable)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.Equal(Delivery.Unreliable, model.Delivery);
    }

    // -- what an archetype declares ------------------------------------------------------------------

    /// <summary>Furniture's shape: nothing of its own, and still an entity the other side hears about.</summary>
    [Fact]
    public void A_replicated_archetype_holding_no_values_is_fine()
        => Assert.Empty(Report("""
            [Archetype]
            public readonly partial struct Subject;
            """));

    [Fact]
    public void A_replicated_archetype_replicates_its_own_values()
    {
        var model = Model("""
            [Archetype(Replication = Replication.OwnershipBased)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.True(model.IsReplicated);
        Assert.True(model.RegistersReplication);
    }

    [Fact]
    public void A_local_archetype_keeps_its_own_values_local()
    {
        var model = Model("""
            [Archetype(Networked = false)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.False(model.IsReplicated);
        Assert.False(model.RegistersReplication);
    }

    [Fact]
    public void An_archetype_carries_its_delivery_on_the_attribute()
    {
        var model = Model("""
            [Archetype(Replication = Replication.OwnershipBased, Delivery = Delivery.Unreliable)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.Equal(Delivery.Unreliable, model.Delivery);
    }

    /// <summary>
    /// Whether the entity gets a network identity is read at create time off the set, so the set has
    /// to say it, and say it apart from any include whose set holds the same components.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void An_archetype_set_carries_its_flag(bool networked)
        => Assert.Contains(
            $"ComponentSet.Archetype({(networked ? "true" : "false")}, ",
            Generated($$"""
                [ArchetypeMixin(Replication.OwnershipBased)]
                public readonly partial struct Telemetry
                {
                    public partial int Ticks { get; set; }
                }

                [Archetype(Networked = {{(networked ? "true" : "false")}})]
                [Include(typeof(Telemetry))]
                public readonly partial struct Subject;
                """));

    /// <summary>A mixin's set is shared by every shape that includes it, so it says nothing either way.</summary>
    [Fact]
    public void A_mixin_set_is_not_an_archetype_set()
    {
        var generated = Generated("""
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }
            }
            """);

        Assert.DoesNotContain("ComponentSet.Archetype(", generated);
    }

    // -- collections -------------------------------------------------------------------------------

    /// <summary>
    /// Handing a caller the collection itself lets them change it without the change being sent,
    /// so a replicated shape has to keep it private and let the generated members carry it.
    /// </summary>
    [Fact]
    public void A_replicated_collection_that_is_reachable_as_a_whole_is_refused()
    {
        var reported = Report("""
            [ArchetypeMixin(Replication.OwnershipBased)]
            public readonly partial struct Subject
            {
                public partial global::ReadyM.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

        Assert.Contains(reported, d => d.Id == "READYM011");
    }

    [Fact]
    public void A_private_replicated_collection_is_accepted()
    {
        var reported = Report("""
            [ArchetypeMixin(Replication.OwnershipBased)]
            public readonly partial struct Subject
            {
                private partial global::ReadyM.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id == "READYM011");
    }

    /// <summary>
    /// A shape that does not replicate has no generated members to reach a collection through, so
    /// what it would hold is a handle to memory and no way to work it.
    /// </summary>
    [Fact]
    public void A_local_shape_cannot_hold_a_collection()
        => AssertReports("READYM018", """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial global::ReadyM.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

    /// <summary>Including keeping it to itself, which was the shape that used to slip through.</summary>
    [Fact]
    public void Nor_a_private_one()
        => AssertReports("READYM018", """
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                private partial global::ReadyM.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

    /// <summary>It compiled before, and the generated serializer threw the first time the entity was sent.</summary>
    [Theory]
    [InlineData("global::System.Collections.Generic.List<int>")]
    [InlineData("global::System.Collections.Generic.Dictionary<string, int>")]
    [InlineData("global::ReadyM.Native.Container.NativeList<global::Friflo.Engine.ECS.Entity>")]
    public void A_replicated_shape_cannot_hold_a_value_it_cannot_send(string type)
        => AssertReports("READYM046", $$"""
            [ArchetypeMixin(Replication.ServerAuthoritative)]
            public readonly partial struct Subject
            {
                private partial {{type}} Items { get; set; }
            }
            """);

    [Fact]
    public void A_local_shape_can_hold_a_plain_collection()
    {
        var reported = Report("""
            [ArchetypeMixin(Replication.None)]
            public readonly partial struct Subject
            {
                public partial global::System.Collections.Generic.List<int> Items { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id is "READYM046" or "READYM018");
    }

    [Fact]
    public void A_replicated_shape_can_hold_what_it_sends()
    {
        var reported = Report("""
            [ArchetypeMixin(Replication.ServerAuthoritative)]
            public readonly partial struct Subject
            {
                public partial int Count { get; set; }
                public partial global::ReadyM.Native.Container.NativeString64 Name { get; set; }
                private partial global::ReadyM.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id == "READYM046");
    }
}
