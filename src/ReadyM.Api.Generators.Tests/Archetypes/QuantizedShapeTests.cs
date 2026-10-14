using System.Numerics;
using System.Reflection;
using LiteNetLib.Utils;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;
using static ReadyM.Api.Generators.Tests.DeriveTestAssert;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// [Quantize], [QuantizeAngle] and [HalfPrecision] on a shape's property: honoured where the shape's own component
/// replicates, refused where they would do nothing.
public class QuantizedShapeTests(ITestOutputHelper output)
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
        using System.Numerics;
        using Friflo.Engine.ECS;

        namespace Core;

        public struct PoseComponent : IComponent
        {
            public Vector3 Position;
        }
        """;

    private const string ReplicatedMixin =
        """
        [ArchetypeMixin]
        [Replicated]
        [Propagates(Propagation.OwnershipBased)]
        public readonly partial struct Pose
        {
            [Quantize(-1048576f, 1048576f, 24)] public partial Vector3 Position { get; set; }
            [Quantize(-1f, 1f, 16)] public partial Vector2 Axis { get; set; }
            [QuantizeAngle(16)] public partial float Yaw { get; set; }
            [HalfPrecision] public partial float Speed { get; set; }
            [HalfPrecision] public partial Vector3 Velocity { get; set; }
            public partial float Plain { get; set; }
        }
        """;

    private static string Source(string shape) =>
        $$"""
          using System.Numerics;
          using ReadyM.Api.Multiplayer.Generators;
          using ReadyM.SDK.Attributes;

          namespace Mod;

          {{shape}}
          """;

    [Fact]
    public void Marked_properties_on_a_replicated_mixin_go_over_the_wire_in_their_encoding()
    {
        var generated = Generated(ReplicatedMixin);

        Assert.DoesNotContain(Run(ReplicatedMixin).OutputDiagnostics, d => d.Id == "CS0592");
        Assert.Contains("Quantization.Write(writer, position, -1048576.0f, 1048576.0f, 24);", generated);
        Assert.Contains("Quantization.ReadVector2(reader, -1.0f, 1.0f, 16)", generated);
        Assert.Contains("Quantization.WriteAngle(writer, yaw, 16);", generated);
        Assert.Contains("Quantization.WriteHalf(writer, speed);", generated);
        Assert.Contains("Quantization.ReadHalfVector3(reader)", generated);
        Assert.Contains("Quantization.Unchanged(position, value, -1048576.0f, 1048576.0f, 24)", generated);
        Assert.Contains("Quantization.UnchangedAngle(yaw, value, 16)", generated);
    }

    [Fact]
    public void Marked_properties_on_a_replicated_archetype_go_over_the_wire_in_their_encoding()
    {
        var generated = Generated(
            """
            [Archetype(replicated: true)]
            [Propagates(Propagation.OwnershipBased)]
            public readonly partial struct Wolf
            {
                [Quantize(-1048576f, 1048576f, 24)] public partial Vector3 Position { get; set; }
                [QuantizeAngle(8)] public partial float Yaw { get; set; }
            }
            """);

        Assert.Contains("Quantization.Write(writer, position, -1048576.0f, 1048576.0f, 24);", generated);
        Assert.Contains("Quantization.WriteAngle(writer, yaw, 8);", generated);
    }

    [Fact]
    public void A_delta_of_a_replicated_mixin_takes_the_mask_and_each_encoding()
    {
        var component = Activator.CreateInstance(CompileComponentOf(ReplicatedMixin, "Pose"))!;
        Invoke(component, "ClearDirty");
        var maskBytes = WriteDelta(component).Length;

        SetProperty(component, "Position", new Vector3(10f, -20f, 30f));
        SetProperty(component, "Axis", new Vector2(0.5f, -0.5f));
        SetProperty(component, "Yaw", 90f);
        SetProperty(component, "Speed", 350f);
        SetProperty(component, "Velocity", new Vector3(100f, 0f, -100f));
        SetProperty(component, "Plain", 7f);

        // 3 x 3 position, 2 x 2 axis, 2 yaw, 2 speed, 3 x 2 velocity, 4 plain
        Assert.Equal(maskBytes + 9 + 4 + 2 + 2 + 6 + 4, WriteDelta(component).Length);
    }

    [Fact]
    public void A_mark_on_a_shape_over_an_explicit_component_is_refused()
    {
        var reported = Report(
            """
            [ArchetypeMixin]
            [ExplicitComponent(typeof(Core.PoseComponent))]
            public readonly partial struct Pose
            {
                [Quantize(-1048576f, 1048576f, 24)] public partial Vector3 Position { get; set; }
            }
            """);

        var error = Assert.Single(reported, d => d.Id == "READYM039");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("Core.PoseComponent", error.GetMessage());
        Assert.Contains("[Quantize]", error.GetMessage());
    }

    [Theory]
    [InlineData("[Quantize(0f, 1f, 8)] public partial float Alpha { get; set; }", "Quantize")]
    [InlineData("[QuantizeAngle(16)] public partial float Alpha { get; set; }", "QuantizeAngle")]
    [InlineData("[HalfPrecision] public partial float Alpha { get; set; }", "HalfPrecision")]
    public void A_mark_on_a_shape_nothing_sends_is_refused(string property, string attribute)
    {
        var reported = Report(
            $$"""
              [ArchetypeMixin]
              public readonly partial struct Blend
              {
                  {{property}}
              }
              """);

        var error = Assert.Single(reported, d => d.Id == "READYM040");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains($"[{attribute}]", error.GetMessage());
    }

    [Fact]
    public void Marks_on_a_replicated_shape_report_neither()
    {
        Assert.DoesNotContain(Report(ReplicatedMixin), d => d.Id is "READYM039" or "READYM040");
    }

    [Fact]
    public void A_mark_on_an_unsupported_type_is_the_usual_compile_error()
    {
        var generated = Generated(
            """
            [ArchetypeMixin]
            [Replicated]
            public readonly partial struct Level
            {
                [HalfPrecision] public partial int Value { get; set; }
            }
            """);

        Assert.Contains("[HalfPrecision] only applies to float, Vector2, Vector3 and Vector4", generated);
        Assert.DoesNotContain("Quantization.WriteHalf", generated);
    }

    // ---

    private static NetDataWriter WriteDelta(object component)
    {
        var writer = new NetDataWriter();
        Invoke(component, "WriteDelta", writer);
        return writer;
    }

    private Type CompileComponentOf(string shape, string shapeName)
    {
        var result = Run(shape);
        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        return assembly.GetTypes().Single(type =>
            type.Namespace == "Mod"
            && type.Name.Contains(shapeName)
            && type.GetMethod("WriteDelta") is not null);
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
