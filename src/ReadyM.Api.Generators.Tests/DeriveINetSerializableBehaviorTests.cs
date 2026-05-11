using Xunit;
using static ReadyM.Api.Generators.Tests.DeriveTestAssert;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveINetSerializableBehaviorTests(ITestOutputHelper output)
{
    [Fact]
    public void PrimitiveCoverageStruct_SerializeAndDeserialize_BehaveAsExpected()
    {
        const string source = """
using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum SmallState : byte
{
    None = 0,
    Ready = 1,
    Done = 2
}

public enum LargeState : int
{
    Zero = 0,
    One = 1,
    Two = 2
}

[DeriveINetSerializable]
public partial struct PrimitiveCoverageSerializable
{
    private bool _flag;
    private sbyte _signedByte;
    private byte _unsignedByte;
    private short _signedShort;
    private ushort _unsignedShort;
    private int _signedInt;
    private uint _unsignedInt;
    private long _signedLong;
    private ulong _unsignedLong;
    private float _floatValue;
    private double _doubleValue;
    private char _letter;
    private string _name;
    private SmallState _smallState;
    private LargeState _largeState;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.PrimitiveCoverageSerializable");
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        SetField(instance, "_flag", true);
        SetField(instance, "_signedByte", (sbyte)-12);
        SetField(instance, "_unsignedByte", (byte)200);
        SetField(instance, "_signedShort", (short)-1234);
        SetField(instance, "_unsignedShort", (ushort)54321);
        SetField(instance, "_signedInt", -123456789);
        SetField(instance, "_unsignedInt", 3456789012u);
        SetField(instance, "_signedLong", -1234567890123456789L);
        SetField(instance, "_unsignedLong", 12345678901234567890UL);
        SetField(instance, "_floatValue", 1.25f);
        SetField(instance, "_doubleValue", 10.5d);
        SetField(instance, "_letter", 'Z');
        SetField(instance, "_name", "Alpha");
        SetField(instance, "_smallState", ParseEnum(assembly, "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done"));
        SetField(instance, "_largeState", ParseEnum(assembly, "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two"));

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(type);
        Assert.NotNull(deserialized);

        AssertFieldNotValue(deserialized, "_flag", true);
        AssertFieldNotValue(deserialized, "_signedByte", (sbyte)-12);
        AssertFieldNotValue(deserialized, "_unsignedByte", (byte)200);
        AssertFieldNotValue(deserialized, "_signedShort", (short)-1234);
        AssertFieldNotValue(deserialized, "_unsignedShort", (ushort)54321);
        AssertFieldNotValue(deserialized, "_signedInt", -123456789);
        AssertFieldNotValue(deserialized, "_unsignedInt", 3456789012u);
        AssertFieldNotValue(deserialized, "_signedLong", -1234567890123456789L);
        AssertFieldNotValue(deserialized, "_unsignedLong", 12345678901234567890UL);
        AssertFieldNotValue(deserialized, "_floatValue", 1.25f);
        AssertFieldNotValue(deserialized, "_doubleValue", 10.5d);
        AssertFieldNotValue(deserialized, "_letter", 'Z');
        AssertFieldNotValue(deserialized, "_name", "Alpha");
        AssertEnumFieldNotValue(assembly, deserialized, "_smallState", "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done");
        AssertEnumFieldNotValue(assembly, deserialized, "_largeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two");

        InvokeDeserialize(deserialized, serializedBytes);

        AssertFieldValue(deserialized, "_flag", true);
        AssertFieldValue(deserialized, "_signedByte", (sbyte)-12);
        AssertFieldValue(deserialized, "_unsignedByte", (byte)200);
        AssertFieldValue(deserialized, "_signedShort", (short)-1234);
        AssertFieldValue(deserialized, "_unsignedShort", (ushort)54321);
        AssertFieldValue(deserialized, "_signedInt", -123456789);
        AssertFieldValue(deserialized, "_unsignedInt", 3456789012u);
        AssertFieldValue(deserialized, "_signedLong", -1234567890123456789L);
        AssertFieldValue(deserialized, "_unsignedLong", 12345678901234567890UL);
        AssertFieldValue(deserialized, "_floatValue", 1.25f);
        AssertFieldValue(deserialized, "_doubleValue", 10.5d);
        AssertFieldValue(deserialized, "_letter", 'Z');
        AssertFieldValue(deserialized, "_name", "Alpha");
        AssertEnumFieldValue(assembly, deserialized, "_smallState", "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done");
        AssertEnumFieldValue(assembly, deserialized, "_largeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two");
    }

    [Fact]
    public void NestedSerializableStruct_AndManagedReferenceField_RoundTripAsExpected()
    {
        const string source = """
using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct CustomValue : IEquatable<CustomValue>
{
    public int Id;
    public float Amount;

    public readonly bool Equals(CustomValue other)
        => Id == other.Id && Math.Abs(Amount - other.Amount) < 0.0001f;

    public override readonly bool Equals(object? obj)
        => obj is CustomValue other && Equals(other);

    public override readonly int GetHashCode()
        => HashCode.Combine(Id, Amount);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Amount);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Amount = reader.GetFloat();
    }
}

public sealed class ManagedPayload
{
    public int Revision;
    public string? Name;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Revision);
        writer.Put(Name);
    }

    public void Deserialize(NetDataReader reader)
    {
        Revision = reader.GetInt();
        Name = reader.GetString();
    }
}

[DeriveINetSerializable]
public partial struct ComplexSerializable
{
    private CustomValue _value;
    private ManagedPayload _payload;
    private int _count;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.ComplexSerializable");
        var customValueType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.CustomValue");
        var managedPayloadType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.ManagedPayload");

        Assert.NotNull(type);
        Assert.NotNull(customValueType);
        Assert.NotNull(managedPayloadType);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        SetField(instance, "_value", CreateCustomValue(customValueType, id: 10, amount: 2.5f));
        SetField(instance, "_payload", CreateManagedPayload(managedPayloadType, revision: 7, name: "Alpha"));
        SetField(instance, "_count", 123);

        var serializedBytes = InvokeSerialize(instance);

        var managedPayloadEmpty = Activator.CreateInstance(managedPayloadType);
        var deserialized = Activator.CreateInstance(type);
        Assert.NotNull(deserialized);
        SetField(deserialized!, "_payload", managedPayloadEmpty);

        AssertCustomValueNotValue(customValueType, GetField<object>(deserialized, "_value"), expectedId: 10, expectedAmount: 2.5f);
        AssertManagedPayloadNotValue(managedPayloadType, GetField<object>(deserialized, "_payload"), expectedRevision: 7, expectedName: "Alpha");
        AssertFieldNotValue(deserialized, "_count", 123);

        InvokeDeserialize(deserialized, serializedBytes);

        AssertCustomValueValue(customValueType, GetField<object>(deserialized, "_value"), expectedId: 10, expectedAmount: 2.5f);
        AssertManagedPayloadValue(managedPayloadType, GetField<object>(deserialized, "_payload"), expectedRevision: 7, expectedName: "Alpha");
        AssertFieldValue(deserialized, "_count", 123);
    }

    [Fact]
    public void ZeroFieldStruct_SerializeAndDeserialize_BehaveAsExpected()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetSerializable]
public partial struct ZeroFieldSerializable
{
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.ZeroFieldSerializable");
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        var serializedBytes = InvokeSerialize(instance);
        Assert.Empty(serializedBytes);

        var deserialized = Activator.CreateInstance(type);
        Assert.NotNull(deserialized);

        InvokeDeserialize(deserialized, serializedBytes);
    }

    [Fact]
    public void SingleFieldStruct_SerializeAndDeserialize_BehaveAsExpected()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetSerializable]
public partial struct SingleFieldSerializable
{
    private int _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.SingleFieldSerializable");
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        SetField(instance, "_value", 123);

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(type);
        Assert.NotNull(deserialized);

        AssertFieldNotValue(deserialized, "_value", 123);

        InvokeDeserialize(deserialized, serializedBytes);

        AssertFieldValue(deserialized, "_value", 123);
    }

    [Fact]
    public void NonPrivateFieldMappingModes_IncludeConfiguredAccessLevelsOnly()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetSerializable(mode: SerializableMode.MapFields | SerializableMode.MapPublic | SerializableMode.MapInternal)]
public partial struct NonPrivateMappedFieldsSerializable
{
    private int _privateValue;
    public int _publicValue;
    internal int _internalValue;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.DoesNotContain("_privateValue", generatedText);
        Assert.Contains("_publicValue", generatedText);
        Assert.Contains("_internalValue", generatedText);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.NonPrivateMappedFieldsSerializable");
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        SetField(instance, "_publicValue", 10);
        SetField(instance, "_internalValue", 20);

        var bytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(type);
        Assert.NotNull(deserialized);

        InvokeDeserialize(deserialized, bytes);

        AssertFieldValue(deserialized, "_publicValue", 10);
        AssertFieldValue(deserialized, "_internalValue", 20);
    }

    [Fact]
    public void PropertyMappingMode_AutoProperties_AreSerializedAndDeserialized()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetSerializable(mode: SerializableMode.MapProperties | SerializableMode.MapPublic | SerializableMode.MapInternal)]
public partial struct PropertyMappedSerializable
{
    public int PublicValue { get; set; }
    internal int InternalValue { get; set; }
    private int PrivateValue { get; set; }

    public int _publicOtherValue { get; set; }
    internal int internalOtherValue { get; set; }
    private int privateOtherValue { get; set; }
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("PublicValue", generatedText);
        Assert.Contains("InternalValue", generatedText);
        Assert.Contains("_publicOtherValue", generatedText);
        Assert.Contains("internalOtherValue", generatedText);
        Assert.DoesNotContain("PrivateValue", generatedText);
        Assert.DoesNotContain("privateOtherValue", generatedText);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.PropertyMappedSerializable");
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        SetProperty(instance, "PublicValue", 100);
        SetProperty(instance, "InternalValue", 200);
        SetProperty(instance, "_publicOtherValue", 300);
        SetProperty(instance, "internalOtherValue", 400);

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(type);
        Assert.NotNull(deserialized);

        InvokeDeserialize(deserialized, serializedBytes);

        AssertPropertyValue(deserialized, "PublicValue", 100);
        AssertPropertyValue(deserialized, "InternalValue", 200);
        AssertPropertyValue(deserialized, "_publicOtherValue", 300);
        AssertPropertyValue(deserialized, "internalOtherValue", 400);
    }

    [Fact]
    public void GeneratedShape_ForRepresentativeSerializable_UsesExpectedPrimitiveEnumAndNestedCalls()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum TinyState : byte
{
    A = 0,
    B = 1
}

public struct NestedValue
{
    public int Count;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Count);
    }

    public void Deserialize(NetDataReader reader)
    {
        Count = reader.GetInt();
    }
}

[DeriveINetSerializable]
public partial struct GeneratedShapeSerializable
{
    private int _count;
    private string? _name;
    private TinyState _state;
    private NestedValue _nested;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("writer.Put(_count);", generatedText);
        Assert.Contains("writer.Put(_name);", generatedText);
        Assert.Contains("writer.Put((byte)_state);", generatedText);
        Assert.Contains("_nested.Serialize(writer);", generatedText);

        Assert.Contains("_count = reader.GetInt();", generatedText);
        Assert.Contains("_name = reader.GetString();", generatedText);
        Assert.Contains("_state = (global::ReadyM.Api.Generators.Tests.TestTypes.TinyState)reader.GetByte();", generatedText);
        Assert.Contains("_nested.Deserialize(reader);", generatedText);
    }

    [Fact]
    public void ManagedStringField_NullAndNonNullValues_RoundTripAsExpected()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetSerializable]
public partial struct NullableStringSerializable
{
    private string? _name;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.NullableStringSerializable");
        Assert.NotNull(type);

        var withNull = Activator.CreateInstance(type);
        Assert.NotNull(withNull);

        AssertNullFieldValue(withNull, "_name");
        var nullBytes = InvokeSerialize(withNull);

        var nullRoundTrip = Activator.CreateInstance(type);
        Assert.NotNull(nullRoundTrip);

        InvokeDeserialize(nullRoundTrip, nullBytes);
        
        // NOTE: This is the current semantics for string deserialization
        AssertFieldValue(nullRoundTrip, "_name", "");

        var withValue = Activator.CreateInstance(type);
        Assert.NotNull(withValue);

        SetField(withValue, "_name", "Alpha");
        var valueBytes = InvokeSerialize(withValue);

        var valueRoundTrip = Activator.CreateInstance(type);
        Assert.NotNull(valueRoundTrip);

        InvokeDeserialize(valueRoundTrip, valueBytes);
        AssertFieldValue(valueRoundTrip, "_name", "Alpha");
    }

    // ---
    
    private static object CreateManagedPayload(Type payloadType, int revision, string? name)
    {
        var instance = Activator.CreateInstance(payloadType);
        Assert.NotNull(instance);

        var revisionField = payloadType.GetField("Revision");
        var nameField = payloadType.GetField("Name");

        Assert.NotNull(revisionField);
        Assert.NotNull(nameField);

        revisionField.SetValue(instance, revision);
        nameField.SetValue(instance, name);

        return instance;
    }

    private static void AssertManagedPayloadValue(Type payloadType, object? actual, int expectedRevision, string? expectedName)
    {
        Assert.NotNull(actual);

        var revisionField = payloadType.GetField("Revision");
        var nameField = payloadType.GetField("Name");

        Assert.NotNull(revisionField);
        Assert.NotNull(nameField);

        Assert.Equal(expectedRevision, (int)revisionField.GetValue(actual)!);
        Assert.Equal(expectedName, (string?)nameField.GetValue(actual));
    }

    private static void AssertManagedPayloadNotValue(Type payloadType, object? actual, int expectedRevision, string? expectedName)
    {
        if (actual is null)
        {
            return;
        }

        var revisionField = payloadType.GetField("Revision");
        var nameField = payloadType.GetField("Name");

        Assert.NotNull(revisionField);
        Assert.NotNull(nameField);

        var actualRevision = (int)revisionField.GetValue(actual)!;
        var actualName = (string?)nameField.GetValue(actual);

        Assert.False(actualRevision == expectedRevision && actualName == expectedName);
    }
}