using Microsoft.CodeAnalysis;
using Xunit;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveINetworkedComponentDiagnosticsTests(ITestOutputHelper output)
{
    [Fact]
    public void UnsupportedFieldTypeEmitsCompilationError()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct UnsupportedType
{
    public int Value;
}

[DeriveINetworkedComponent]
public partial struct UnsupportedComponent
{
    private UnsupportedType _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);

        Assert.Contains(
            errors,
            diagnostic =>
                diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void UnsupportedPropertyTypeEmitsCompilationError()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct UnsupportedType
{
    public int Value;
}

[DeriveINetworkedComponent(mode: SerializableMode.MapProperties | SerializableMode.MapPublic)]
public partial struct UnsupportedPropertyComponent
{
    public UnsupportedType Value { get; set; }
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void ReadonlyFieldEmitsCompilationError()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct ReadonlyFieldComponent
{
    private readonly int _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void ReadonlyCustomSerializableFieldEmitsCompilationError()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct CustomValue
{
    public int Value;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetInt();
    }
}

[DeriveINetworkedComponent]
public partial struct ReadonlyCustomSerializableFieldComponent
{
    private readonly CustomValue _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void InitOnlyPropertyEmitsCompilationError()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(mode: SerializableMode.MapProperties | SerializableMode.MapPublic)]
public partial struct InitOnlyPropertyComponent
{
    public int Value { get; init; }
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void GetterOnlyPropertyEmitsCompilationError()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(mode: SerializableMode.MapProperties | SerializableMode.MapPublic)]
public partial struct GetterOnlyPropertyComponent
{
    public int Value => 123;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void MoreThanSixtyFourMappedFieldsEmitsCompilationError()
    {
        var fields = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, 65).Select(i => $"    private int _value{i};"));

        var source = $$"""
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct TooManyFieldsComponent
{
{{fields}}
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("more than 64", StringComparison.OrdinalIgnoreCase) || diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void MoreThanSixtyFourMappedPropertiesEmitsCompilationError()
    {
        var properties = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, 65).Select(i => $"    public int Value{i} {{ get; set; }}"));

        var source = $$"""
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(mode: SerializableMode.MapProperties | SerializableMode.MapPublic)]
public partial struct TooManyPropertiesComponent
{
{{properties}}
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("more than 64", StringComparison.OrdinalIgnoreCase) || diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void MoreThanSixtyFourMappedMembersAcrossFieldsAndPropertiesEmitsCompilationError()
    {
        var fields = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, 33).Select(i => $"    private int _value{i};"));

        var properties = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, 32).Select(i => $"    public int Value{i} {{ get; set; }}"));

        var source = $$"""
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(mode: SerializableMode.MapFieldsAndProperties | SerializableMode.MapPrivate | SerializableMode.MapPublic)]
public partial struct TooManyMixedMembersComponent
{
{{fields}}
{{properties}}
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("more than 64", StringComparison.OrdinalIgnoreCase) || diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void FieldWithoutDeserializeMethodEmitsCompilationError()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct SerializeOnlyValue
{
    public int Value;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }
}

[DeriveINetworkedComponent]
public partial struct MissingDeserializeComponent
{
    private SerializeOnlyValue _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("#error"));
    }

    [Fact]
    public void ExplicitDirtyMaskTypeWithTooFewBitsEmitsCompilationError()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(emitDirtyMask: false)]
public partial struct ExplicitByteDirtyMaskTooSmallComponent
{
  private byte _dirtyMask;

  private int _value0;
  private int _value1;
  private int _value2;
  private int _value3;
  private int _value4;
  private int _value5;
  private int _value6;
  private int _value7;
  private int _value8;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);

        Assert.Contains(
            errors,
            diagnostic => diagnostic.ToString().Contains("#error"));
    }
    
    [Fact]
    public void FieldWithoutSerializeMethodEmitsCompilationError()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct DeserializeOnlyValue
{
    public int Value;

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetInt();
    }
}

[DeriveINetworkedComponent]
public partial struct MissingSerializeComponent
{
    private DeserializeOnlyValue _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, diagnostic => diagnostic.ToString().Contains("#error"));
    }
}