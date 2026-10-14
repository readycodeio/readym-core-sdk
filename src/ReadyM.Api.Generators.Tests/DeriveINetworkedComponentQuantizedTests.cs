using System.Numerics;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Multiplayer.Serialization;
using Xunit;
using static ReadyM.Api.Generators.Tests.DeriveTestAssert;

namespace ReadyM.Api.Generators.Tests;

// Fields marked [Quantize], [QuantizeAngle] or [HalfPrecision] go over the wire in their encoding, and only count as
// changed when the encoding does
public sealed class DeriveINetworkedComponentQuantizedTests(ITestOutputHelper output)
{
    private const string Source = """
using System.Numerics;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct QuantizedComponent : INetworkedComponent
{
    [Quantize(-1048576f, 1048576f, 24)] private Vector3 _position;
    [QuantizeAngle(16)] private float _yaw;
    [HalfPrecision] private Vector3 _velocity;
    [Quantize(0f, 1f, 8)] private float _alpha;
    private float _plain;
}
""";

    private const float PositionStep = 2097152f / 0xFFFFFE;

    [Fact]
    public void A_snapshot_takes_the_encoded_sizes()
    {
        var component = Create(new Vector3(1000.5f, -2000.25f, 300f), 45f, new Vector3(350f, 0f, -12f), 0.5f, 7f);

        // 3 x 3 position, 2 yaw, 3 x 2 velocity, 1 alpha, 4 plain float
        Assert.Equal(9 + 2 + 6 + 1 + 4, InvokeSerialize(component).Length);
    }

    [Fact]
    public void A_snapshot_round_trips_within_each_fields_step()
    {
        var position = new Vector3(123456.78f, -98765.43f, 4321.1f);
        var sent = Create(position, 190f, new Vector3(350.3f, 0.01f, -12.7f), 0.33f, 7.25f);

        var received = CreateEmpty();
        InvokeDeserialize(received, InvokeSerialize(sent));

        var back = GetProperty<Vector3>(received, "Position");
        Assert.InRange(Vector3.Distance(back, position), 0f, PositionStep);
        Assert.Equal(-170f, GetProperty<float>(received, "Yaw"), 2);
        Assert.InRange(Vector3.Distance(GetProperty<Vector3>(received, "Velocity"), new Vector3(350.3f, 0.01f, -12.7f)), 0f, 0.5f);
        Assert.Equal(0.33f, GetProperty<float>(received, "Alpha"), 2);
        Assert.Equal(7.25f, GetProperty<float>(received, "Plain"));
    }

    [Fact]
    public void A_delta_carries_only_the_changed_fields_in_their_encoding()
    {
        var sent = Create(new Vector3(10f, 20f, 30f), 0f, Vector3.Zero, 0f, 0f);
        Invoke(sent, "ClearDirty");

        SetProperty(sent, "Position", new Vector3(11f, 20f, 30f));
        SetProperty(sent, "Yaw", 90f);

        var delta = InvokeWriteDelta(sent);

        // mask byte, then 9 bytes of position and 2 of yaw
        Assert.Equal(1 + 9 + 2, delta.Length);

        var received = Create(new Vector3(10f, 20f, 30f), 0f, Vector3.Zero, 0f, 0f);
        InvokeReadDelta(received, delta);
        Assert.InRange(Vector3.Distance(GetProperty<Vector3>(received, "Position"), new Vector3(11f, 20f, 30f)), 0f, PositionStep);
        Assert.Equal(90f, GetProperty<float>(received, "Yaw"), 2);
    }

    [Fact]
    public void A_change_inside_one_step_neither_marks_the_field_dirty_nor_moves_it()
    {
        // A value decoded from the wire sits in the middle of its step, so a tenth of a step stays in it
        var onGrid = Quantization.Dequantize(Quantization.Quantize(100f, -1048576f, 1048576f, 24), -1048576f, 1048576f, 24);
        var component = Create(new Vector3(onGrid, onGrid, onGrid), 30f, new Vector3(1f, 1f, 1f), 0.5f, 0f);
        Invoke(component, "ClearDirty");
        var stored = GetProperty<Vector3>(component, "Position");

        SetProperty(component, "Position", stored + new Vector3(PositionStep / 10, 0f, 0f));
        SetProperty(component, "Yaw", 30.0001f);
        SetProperty(component, "Velocity", new Vector3(1.0001f, 1f, 1f));
        SetProperty(component, "Alpha", 0.5001f);

        Assert.False(GetProperty<bool>(component, "IsDirty"));
        Assert.Equal(stored, GetProperty<Vector3>(component, "Position"));
    }

    [Fact]
    public void A_change_to_another_step_marks_the_field_dirty()
    {
        var component = Create(new Vector3(100f, 100f, 100f), 30f, Vector3.Zero, 0.5f, 0f);
        Invoke(component, "ClearDirty");

        SetProperty(component, "Position", new Vector3(100f + PositionStep * 2, 100f, 100f));

        Assert.True(GetProperty<bool>(component, "IsDirty"));
    }

    [Theory]
    [InlineData("[Quantize(0f, 1f, 8)] private int _value;", "only applies to float")]
    [InlineData("[Quantize(0f, 1f, 12)] private float _value;", "takes 8, 16 or 24 bits")]
    [InlineData("[Quantize(1f, 0f, 8)] private float _value;", "needs min below max")]
    [InlineData("[QuantizeAngle(16)] private Vector3 _value;", "[QuantizeAngle] only applies to float")]
    [InlineData("[QuantizeAngle(24)] private float _value;", "takes 8 or 16 bits")]
    [InlineData("[HalfPrecision] private int _value;", "[HalfPrecision] only applies")]
    [InlineData("[HalfPrecision, QuantizeAngle(16)] private float _value;", "use only one of")]
    public void A_misused_attribute_is_an_error(string field, string message)
    {
        var source = $$"""
using System.Numerics;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct MisusedComponent : INetworkedComponent
{
    {{field}}
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);
        var generated = string.Join(Environment.NewLine, result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("#error Field _value", generated);
        Assert.Contains(message, generated);
    }

    // ---

    private Type? _componentType;

    private Type CompileComponentType()
    {
        if (_componentType != null)
            return _componentType;

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(Source, output);
        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        return _componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.QuantizedComponent")!;
    }

    private object CreateEmpty()
        => Activator.CreateInstance(CompileComponentType())!;

    private object Create(Vector3 position, float yaw, Vector3 velocity, float alpha, float plain)
    {
        var component = CreateEmpty();
        SetProperty(component, "Position", position);
        SetProperty(component, "Yaw", yaw);
        SetProperty(component, "Velocity", velocity);
        SetProperty(component, "Alpha", alpha);
        SetProperty(component, "Plain", plain);
        return component;
    }
}
