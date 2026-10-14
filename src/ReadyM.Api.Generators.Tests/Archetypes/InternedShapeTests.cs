using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// [Interned] on a shape's property: honoured where the shape's own component replicates, refused where it would do nothing.
public class InternedShapeTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Server.Entities.IEntities).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly,
        typeof(Api.Multiplayer.ECS.Components.INetworkedComponent).Assembly
    ];

    private const string Core =
        """
        using Friflo.Engine.ECS;
        using Yooni.Native.Container;

        namespace Core;

        public struct NamedComponent : IComponent
        {
            public NativeString256 Name;
        }
        """;

    private static string Source(string shape) =>
        $$"""
          using ReadyM.Api.Multiplayer.Generators;
          using ReadyM.SDK.Attributes;
          using Yooni.Native.Container;

          namespace Mod;

          {{shape}}
          """;

    [Fact]
    public void A_marked_string_on_a_replicated_shape_makes_its_component_interned()
    {
        const string shape =
            """
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Gait
            {
                [Interned] public partial NativeString256 Name { get; set; }
            }
            """;
        var generated = Generated(shape);

        // The generator sees a misplaced attribute too, so only the compiler says whether a property may carry it
        Assert.DoesNotContain(Run(shape).OutputDiagnostics, d => d.Id == "CS0592");
        Assert.Contains("global::ReadyM.Api.Multiplayer.Interning.IInternedComponent", generated);
        Assert.Contains("global::ReadyM.Api.Multiplayer.Interning.InternedStringCodec.Write(writer, in ", generated);
        Assert.Contains("global::ReadyM.Api.Multiplayer.Interning.InternedStringCodec.Read(reader, ref ", generated);
    }

    [Fact]
    public void A_marked_list_on_a_replicated_shape_is_interned_element_by_element()
    {
        var generated = Generated(
            """
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Tags
            {
                [Interned] private partial NativeList<NativeString256> Names { get; set; }
            }
            """);

        Assert.Contains("global::ReadyM.Api.Multiplayer.Interning.IInternedComponent", generated);
        Assert.Contains("global::ReadyM.Api.Multiplayer.Interning.InternedStringCodec.Write(writer, in ", generated);
    }

    [Fact]
    public void An_unmarked_replicated_shape_is_not_interned()
    {
        var generated = Generated(
            """
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Gait
            {
                public partial NativeString256 Name { get; set; }
            }
            """);

        Assert.Contains("partial struct", generated);
        Assert.DoesNotContain("Interning", generated);
    }

    [Fact]
    public void A_mark_on_a_shape_over_an_explicit_component_is_refused()
    {
        var reported = Report(
            """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(Core.NamedComponent))]
            public readonly partial struct Named
            {
                [Interned] public partial NativeString256 Name { get; set; }
            }
            """);

        var error = Assert.Single(reported, d => d.Id == "READYM037");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("Core.NamedComponent", error.GetMessage());
    }

    [Fact]
    public void A_mark_on_a_shape_nothing_sends_is_refused()
    {
        var reported = Report(
            """
            [ArchetypeMixin]
            public readonly partial struct Gait
            {
                [Interned] public partial NativeString256 Name { get; set; }
            }
            """);

        var error = Assert.Single(reported, d => d.Id == "READYM038");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("Gait", error.GetMessage());
    }

    [Fact]
    public void A_mark_on_a_replicated_shape_reports_neither()
    {
        var reported = Report(
            """
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Gait
            {
                [Interned] public partial NativeString256 Name { get; set; }
            }
            """);

        Assert.DoesNotContain(reported, d => d.Id is "READYM037" or "READYM038");
    }

    [Fact]
    public void A_mark_on_anything_but_a_string_is_the_usual_compile_error()
    {
        var generated = Generated(
            """
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Level
            {
                [Interned] public partial int Value { get; set; }
            }
            """);

        Assert.Contains("[Interned] only applies to NativeString256", generated);
        Assert.DoesNotContain("IInternedComponent", generated);
    }

    private Diagnostic[] Report(string shape)
        => [.. Run(shape).DriverRunResult.Diagnostics.Where(d => d.Id.StartsWith("READYM"))];

    private string Generated(string shape)
        => string.Concat(Run(shape).GeneratedSyntaxTrees.Select(tree => tree.ToString()));

    private SourceGeneratorTestHelper.GeneratorRunResult Run(string shape)
        => SourceGeneratorTestHelper.RunGenerators(
            [("Core.cs", Core), ("Shape.cs", Source(shape))],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);
}
