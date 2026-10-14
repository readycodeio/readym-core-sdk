using System.Numerics;
using LiteNetLib.Utils;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Multiplayer.Serialization;
using Xunit;
using static ReadyM.Api.Generators.Tests.DeriveTestAssert;

namespace ReadyM.Api.Generators.Tests;

// How many bytes a delta takes for each quantized field, next to the same types sent plainly
public sealed class DeriveINetworkedComponentQuantizedDeltaSizeTests(ITestOutputHelper output)
{
    private const string Source = """
using System.Numerics;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct DeltaSizeComponent : INetworkedComponent
{
    [Quantize(-100f, 100f, 8)] private float _rangeFloat8;
    [Quantize(-100f, 100f, 16)] private float _rangeFloat16;
    [Quantize(-100f, 100f, 24)] private float _rangeFloat24;
    [Quantize(-100f, 100f, 8)] private Vector2 _rangeVector2x8;
    [Quantize(-100f, 100f, 16)] private Vector2 _rangeVector2x16;
    [Quantize(-1048576f, 1048576f, 24)] private Vector3 _rangeVector3x24;
    [Quantize(-100f, 100f, 16)] private Vector3 _rangeVector3x16;

    [QuantizeAngle(8)] private float _angle8;
    [QuantizeAngle(16)] private float _angle16;

    [HalfPrecision] private float _halfFloat;
    [HalfPrecision] private Vector2 _halfVector2;
    [HalfPrecision] private Vector3 _halfVector3;

    private float _plainFloat;
    private Vector2 _plainVector2;
    private Vector3 _plainVector3;
}
""";

    [Theory]
    [InlineData("RangeFloat8", 1)]
    [InlineData("RangeFloat16", 2)]
    [InlineData("RangeFloat24", 3)]
    [InlineData("RangeVector2x8", 2 * 1)]
    [InlineData("RangeVector2x16", 2 * 2)]
    [InlineData("RangeVector3x24", 3 * 3)]
    [InlineData("RangeVector3x16", 3 * 2)]
    [InlineData("Angle8", 1)]
    [InlineData("Angle16", 2)]
    [InlineData("HalfFloat", 2)]
    [InlineData("HalfVector2", 2 * 2)]
    [InlineData("HalfVector3", 3 * 2)]
    [InlineData("PlainFloat", 4)]
    [InlineData("PlainVector2", 2 * 4)]
    [InlineData("PlainVector3", 3 * 4)]
    public void A_delta_with_one_changed_field_takes_the_mask_and_that_fields_encoding(string field, int fieldBytes)
    {
        var component = Clean();

        SetProperty(component, field, ChangedValueFor(component, field));

        var writer = WriteDelta(component);
        Assert.Equal(MaskBytes() + fieldBytes, writer.Length);
        SkipDeltaOfType(component.GetType(), writer.CopyData());
    }

    [Fact]
    public void A_delta_with_every_field_changed_takes_the_mask_and_every_encoding()
    {
        var component = Clean();
        foreach (var field in Fields)
            SetProperty(component, field, ChangedValueFor(component, field));

        // quantized: 1 + 2 + 3 + 2 + 4 + 9 + 6, angles: 1 + 2, halves: 2 + 4 + 6
        const int quantizedBytes = 1 + 2 + 3 + 2 + 4 + 9 + 6 + 1 + 2 + 2 + 4 + 6;
        const int plainBytes = 4 + 8 + 12;

        var writer = WriteDelta(component);
        Assert.Equal(MaskBytes() + quantizedBytes + plainBytes, writer.Length);
        SkipDeltaOfType(component.GetType(), writer.CopyData());
    }

    [Fact]
    public void A_quantized_float_and_vectors_take_less_than_their_plain_counterparts()
    {
        Assert.True(DeltaBytesOf("RangeFloat8") < DeltaBytesOf("PlainFloat"));
        Assert.True(DeltaBytesOf("HalfFloat") < DeltaBytesOf("PlainFloat"));
        Assert.True(DeltaBytesOf("Angle16") < DeltaBytesOf("PlainFloat"));
        Assert.True(DeltaBytesOf("HalfVector2") < DeltaBytesOf("PlainVector2"));
        Assert.True(DeltaBytesOf("RangeVector3x24") < DeltaBytesOf("PlainVector3"));
        Assert.True(DeltaBytesOf("HalfVector3") < DeltaBytesOf("PlainVector3"));
    }

    [Fact]
    public void A_delta_with_only_changes_inside_a_step_takes_only_the_mask()
    {
        // Each field starts in the middle of a step, as a value decoded from the wire does, and moves a fraction of it
        var range8 = Quantization.Dequantize(Quantization.Quantize(10f, -100f, 100f, 8), -100f, 100f, 8);
        var range16 = Quantization.Dequantize(Quantization.Quantize(10f, -100f, 100f, 16), -100f, 100f, 16);
        var world = Quantization.Dequantize(Quantization.Quantize(1000f, -1048576f, 1048576f, 24), -1048576f, 1048576f, 24);
        var angle = Quantization.DequantizeAngle(Quantization.QuantizeAngle(45f, 16), 16);

        var component = Clean();
        SetProperty(component, "RangeFloat8", range8);
        SetProperty(component, "RangeVector2x16", new Vector2(range16, range16));
        SetProperty(component, "RangeVector3x24", new Vector3(world, world, world));
        SetProperty(component, "Angle16", angle);
        SetProperty(component, "HalfFloat", 10f);
        SetProperty(component, "HalfVector2", new Vector2(10f, 10f));
        SetProperty(component, "HalfVector3", new Vector3(10f, 10f, 10f));
        Invoke(component, "ClearDirty");

        SetProperty(component, "RangeFloat8", range8 + 0.1f);
        SetProperty(component, "RangeVector2x16", new Vector2(range16 + 0.0005f, range16 - 0.0005f));
        SetProperty(component, "RangeVector3x24", new Vector3(world + 0.01f, world - 0.01f, world + 0.01f));
        SetProperty(component, "Angle16", angle + 0.001f);
        SetProperty(component, "HalfFloat", 10.001f);
        SetProperty(component, "HalfVector2", new Vector2(10.001f, 9.999f));
        SetProperty(component, "HalfVector3", new Vector3(10.001f, 9.999f, 10.001f));

        Assert.Equal(MaskBytes(), WriteDelta(component).Length);
    }

    [Fact]
    public void A_change_within_the_cutoff_is_not_sent_even_when_its_encoding_changes()
    {
        var component = Clean();
        SetProperty(component, "HalfFloat", 10f);
        SetProperty(component, "HalfVector3", new Vector3(10f, 10f, 10f));
        SetProperty(component, "Angle16", 45f);
        Invoke(component, "ClearDirty");

        // A 16-bit float step at 10 is about 0.008 and a 16-bit angle step about 0.0055 degrees, so these all move the
        // encoding, but none by more than the 0.1 plain fields ignore
        SetProperty(component, "HalfFloat", 10.05f);
        SetProperty(component, "HalfVector3", new Vector3(10.05f, 9.95f, 10f));
        SetProperty(component, "Angle16", 45.05f);
        Assert.Equal(MaskBytes(), WriteDelta(component).Length);

        SetProperty(component, "HalfFloat", 10.2f);
        Assert.Equal(MaskBytes() + 2, WriteDelta(component).Length);
    }

    // ---

    private static readonly string[] Fields =
    [
        "RangeFloat8", "RangeFloat16", "RangeFloat24", "RangeVector2x8", "RangeVector2x16", "RangeVector3x24",
        "RangeVector3x16", "Angle8", "Angle16", "HalfFloat", "HalfVector2", "HalfVector3",
        "PlainFloat", "PlainVector2", "PlainVector3",
    ];

    // A clean component writes only its dirty mask, so that is the mask's size
    private int MaskBytes()
        => WriteDelta(Clean()).Length;

    private int DeltaBytesOf(string field)
    {
        var component = Clean();
        SetProperty(component, field, ChangedValueFor(component, field));
        return WriteDelta(component).Length - MaskBytes();
    }

    private static object ChangedValueFor(object component, string field)
    {
        var type = component.GetType().GetProperty(field)!.PropertyType;
        if (type == typeof(float))
            return 42.5f;
        if (type == typeof(Vector2))
            return new Vector2(10f, -20f);
        if (type == typeof(Vector3))
            return new Vector3(10f, -20f, 30f);

        throw new ArgumentOutOfRangeException(nameof(field), type, "Unexpected field type.");
    }

    private static NetDataWriter WriteDelta(object component)
    {
        var writer = new NetDataWriter();
        Invoke(component, "WriteDelta", writer);
        return writer;
    }

    private object Clean()
    {
        var component = Activator.CreateInstance(CompileComponentType())!;
        Invoke(component, "ClearDirty");
        return component;
    }

    private Type? _componentType;

    private Type CompileComponentType()
    {
        if (_componentType != null)
            return _componentType;

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(Source, output);
        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        return _componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.DeltaSizeComponent")!;
    }
}
