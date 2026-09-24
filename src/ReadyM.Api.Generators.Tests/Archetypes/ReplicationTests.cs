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

    /// A chunk write goes straight into the component array, past the policy and the masks that a
    /// client write has to obey, so a client build does not get one.
    [Fact]
    public void A_client_build_has_no_chunk_setter()
    {
        var generated = GeneratedFor(ReplicatedShape, "Client");

        Assert.DoesNotContain("chunk, int index, int value", generated);
        Assert.Contains("chunk, int index)", generated);
    }

    [Fact]
    public void A_server_build_keeps_its_chunk_setters()
        => Assert.Contains("chunk, int index, int value", GeneratedFor(ReplicatedShape, "Server"));

    /// A build that says nothing is a server, so nothing an existing project has is taken away.
    [Fact]
    public void An_unstated_side_keeps_them()
        => Assert.Contains("chunk, int index, int value", Generated(ReplicatedShape));

    private const string CarryingShape =
        """
        [ArchetypeMixin]
        [Replicated]
        public readonly partial struct Vitals
        {
            public partial int Health { get; set; }
        }

        [Archetype]
        [Include(typeof(Vitals))]
        public readonly partial struct Fighter;
        """;

    /// A value reaching an archetype through [Include] is generated outright, so a client build can
    /// leave the write off it and send a mod to the token instead.
    private const string Writes = "set => global::Mod.VitalsAccessors.SetHealth(_handle, value);";

    /// Both shapes are in one compilation, so the count says which of them kept a write: the mixin
    /// always does, and the archetype including it does only on a server.
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

    /// The mixin's own declaration says { get; set; }, and C# requires the implementing part to
    /// implement every accessor it declares, so this one setter cannot be dropped.
    [Fact]
    public void The_shape_that_declared_it_keeps_its_own_setter()
        => Assert.Contains("set => global::Mod.VitalsAccessors.SetHealth(_handle, value);",
            GeneratedFor(ReplicatedShape, "Client"));

    // -- how a shape says its values travel ---------------------------------------------------------

    /// The declaration lives on the shape; the component carries the contract the runtime looks for.
    [Fact]
    public void A_stated_propagation_reaches_the_component()
        => Assert.Contains(
            "global::ReadyM.Api.Mapping.Tags.IOwnershipBased",
            Generated(
                """
                [ArchetypeMixin]
                [Replicated]
                [Propagates(Propagation.OwnershipBased)]
                public readonly partial struct Vitals
                {
                    public partial int Health { get; set; }
                }
                """));

    /// Saying nothing leaves the component as it was, so no existing shape changes behaviour.
    [Fact]
    public void An_unstated_propagation_says_nothing()
        => Assert.DoesNotContain("ReadyM.Api.Mapping.Tags.I", Generated(ReplicatedShape));

    /// Nothing sends a local shape's values, so it gets plain accessors, no mask and no tokens.
    [Fact]
    public void A_local_shape_is_left_alone()
    {
        var generated = Generated(
            """
            [ArchetypeMixin]
            public readonly partial struct Ambient
            {
                public partial int Temperature { get; set; }
            }
            """);

        Assert.DoesNotContain("ReadyM.Api.Mapping.Tags.I", generated);
        Assert.DoesNotContain("public static class Field", generated);
        Assert.DoesNotContain("WriteDelta", generated);
    }

    /// A replicated shape that says nothing about who may write it is the state everything else
    /// was silently built on, so it is the one the compiler now refuses.
    [Fact]
    public void A_replicated_shape_must_say_how_it_propagates()
        => AssertReports("READYM014", ReplicatedShape);

    /// Nothing sends it, so nobody is kept from writing it and there is nothing to state.
    [Fact]
    public void A_local_shape_must_not_say_how_it_propagates()
        => AssertReports("READYM016", """
            [ArchetypeMixin]
            [Propagates(Propagation.Both)]
            public readonly partial struct Ambient
            {
                public partial int Temperature { get; set; }
            }
            """);

    /// It holds nothing of its own, so it can never replicate, so it has nothing to state either.
    [Fact]
    public void An_archetype_that_only_includes_must_not_say_how_it_propagates()
        => AssertReports("READYM016", """
            [ArchetypeMixin]
            public readonly partial struct Ambient
            {
                public partial int Temperature { get; set; }
            }

            [Archetype]
            [Include(typeof(Ambient))]
            [Propagates(Propagation.Both)]
            public readonly partial struct Region;
            """);

    /// The component a shape does not own carries the contract, and the runtime reads it there.
    [Fact]
    public void A_shape_over_a_borrowed_component_must_not_state_propagation()
        => AssertReports("READYM015", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.LocalComponent))]
            [Propagates(Propagation.OwnershipBased)]
            public readonly partial struct Meta
            {
                public partial int Value { get; set; }
            }
            """);

    /// Even agreeing with it, because two places to say it are two places to change it.
    [Fact]
    public void Nor_state_the_propagation_the_component_already_carries()
        => AssertReports("READYM015", """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(global::Core.OwnedComponent))]
            [Propagates(Propagation.OwnershipBased)]
            public readonly partial struct Owned
            {
                public partial int Value { get; set; }
            }
            """);


    /// The field a value is held in is its name with a lower first letter, which can land on a
    /// keyword. Sealed, Class and Event are ordinary things for a mod to call a value.
    [Fact]
    public void A_value_whose_field_would_be_a_keyword_is_escaped()
    {
        var generated = Generated("""
            [ArchetypeMixin]
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

    /// The same for one that replicates, where the field is reached from more places.
    [Fact]
    public void And_escaped_on_a_shape_that_replicates()
        => Assert.Contains("@sealed", Generated("""
            [ArchetypeMixin]
            [Replicated]
            [Propagates(Propagation.OwnershipBased)]
            public readonly partial struct Subject
            {
                public partial int Sealed { get; set; }
            }
            """));

    // -- what a shape asks to run as its entities appear -------------------------------------------

    /// An archetype holding nothing of its own is found by its marker, so that is what the handler
    /// hangs on. There is no component to key it by otherwise.
    [Fact]
    public void A_create_handler_on_a_marker_archetype_hangs_on_the_marker()
        => Assert.Contains(
            "CreateHandlerRegistry.Register<global::Mod.SubjectArchetypeMarker>",
            Generated("""
                [Archetype]
                public readonly partial struct Subject
                {
                    [CreateHandler]
                    private void OnCreated() { }
                }
                """));

    /// What it asks for comes from the game's services, resolved as it runs rather than before.
    [Fact]
    public void A_create_handler_is_handed_what_it_asked_for()
        => Assert.Contains(
            "Services.Resolve<global::Core.LocalComponent>()",
            Generated("""
                [ArchetypeMixin]
                public readonly partial struct Subject
                {
                    public partial int Value { get; set; }

                    [CreateHandler]
                    private void OnCreated(global::Core.LocalComponent needed) { }
                }
                """));

    /// There is nobody to hand a result to, and nothing for a static one to run on.
    [Fact]
    public void A_create_handler_returning_something_is_refused()
        => AssertReports("READYM020", """
            [ArchetypeMixin]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }

                [CreateHandler]
                private int OnCreated() => 1;
            }
            """);

    /// The order between two would be nobody's to say, so the shape keeps one.
    [Fact]
    public void A_shape_declaring_two_create_handlers_is_refused()
        => AssertReports("READYM021", """
            [ArchetypeMixin]
            public readonly partial struct Subject
            {
                public partial int Value { get; set; }

                [CreateHandler]
                private void First() { }

                [CreateHandler]
                private void Second() { }
            }
            """);

    /// The mirror of a create handler, and its registration sits inside the shape the same way.
    [Fact]
    public void A_shape_registers_what_it_asked_to_run_as_one_of_its_entities_goes()
        => Assert.Contains(
            "DeleteHandlerRegistry.Register<global::Mod.SubjectArchetypeMarker>",
            Generated("""
                [Archetype]
                public readonly partial struct Subject
                {
                    [DeleteHandler]
                    private void OnDeleted() { }
                }
                """));

    /// A shape may declare both, and they register separately.
    [Fact]
    public void A_shape_may_be_told_about_both_ends()
    {
        var generated = Generated("""
            [Archetype]
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
            [ArchetypeMixin]
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
            [ArchetypeMixin]
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
    /// The component decides whether it crosses the wire, and this one does not.
    [Fact]
    public void A_shape_over_a_local_component_must_not_ask_to_replicate()
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

    /// Nor when it does, because then the shape is asking for something already true of it.
    [Fact]
    public void Nor_when_the_component_is_already_networked()
    {
        var reported = Report($$"""
            [ArchetypeMixin]
            [Replicated]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Subject;
            """);

        Assert.Contains(reported, d => d.Id == "READYM010");
    }

    /// Saying nothing is how a shape over a networked component replicates, and it still does.
    [Fact]
    public void A_shape_over_a_networked_component_replicates_without_asking()
    {
        var reported = Report($$"""
            [ArchetypeMixin]
            [ExplicitComponent(typeof({{Networked}}))]
            public readonly partial struct Subject;
            """);

        Assert.DoesNotContain(reported, d => d.Id.StartsWith("READYM"));
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
    /// what it would hold is a handle to memory and no way to work it.
    [Fact]
    public void A_local_shape_cannot_hold_a_collection()
        => AssertReports("READYM018", """
            [ArchetypeMixin]
            public readonly partial struct Subject
            {
                public partial global::Yooni.Native.Container.NativeList<int> Items { get; set; }
            }
            """);

    /// Including keeping it to itself, which was the shape that used to slip through.
    [Fact]
    public void Nor_a_private_one()
        => AssertReports("READYM018", """
            [ArchetypeMixin]
            public readonly partial struct Subject
            {
                private partial global::Yooni.Native.Container.NativeList<int> Items { get; set; }
            }
            """);
}
