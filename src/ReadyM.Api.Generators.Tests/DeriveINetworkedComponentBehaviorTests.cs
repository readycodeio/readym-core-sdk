using Friflo.Json.Fliox.Transform.Query.Ops;
using LiteNetLib.Utils;
using Xunit;
using Yooni.Native.LowLevel;
using static ReadyM.Api.Generators.Tests.DeriveTestAssert;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveINetworkedComponentBehaviorTests(ITestOutputHelper output)
{
    [Fact]
    public void PrimitiveCoverageComponent_AllGeneratedMethods_BehaveAsExpected()
    {
        const string source = """
using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;

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

[DeriveINetworkedComponent]
public partial struct PrimitiveCoverageComponent : INetworkedComponent
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

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.PrimitiveCoverageComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        SetProperty(instance, "Flag", true);
        SetProperty(instance, "SignedByte", (sbyte)-12);
        SetProperty(instance, "UnsignedByte", (byte)200);
        SetProperty(instance, "SignedShort", (short)-1234);
        SetProperty(instance, "UnsignedShort", (ushort)54321);
        SetProperty(instance, "SignedInt", -123456789);
        SetProperty(instance, "UnsignedInt", 3456789012u);
        SetProperty(instance, "SignedLong", -1234567890123456789L);
        SetProperty(instance, "UnsignedLong", 12345678901234567890UL);
        SetProperty(instance, "FloatValue", 1.25f);
        SetProperty(instance, "DoubleValue", 10.5d);
        SetProperty(instance, "Letter", 'Z');
        SetProperty(instance, "Name", "Alpha");
        SetProperty(instance, "SmallState", ParseEnum(assembly, "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done"));
        SetProperty(instance, "LargeState", ParseEnum(assembly, "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two"));

        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "FloatValue", 1.30f);
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "DoubleValue", 10.55d);
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "FloatValue", 1.50f);
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "DoubleValue", 10.8d);
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        var serializedBytes = InvokeSerialize(instance);
        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        AssertPropertyNotValue(deserialized, "Flag", true);
        AssertPropertyNotValue(deserialized, "SignedByte", (sbyte)-12);
        AssertPropertyNotValue(deserialized, "UnsignedByte", (byte)200);
        AssertPropertyNotValue(deserialized, "SignedShort", (short)-1234);
        AssertPropertyNotValue(deserialized, "UnsignedShort", (ushort)54321);
        AssertPropertyNotValue(deserialized, "SignedInt", -123456789);
        AssertPropertyNotValue(deserialized, "UnsignedInt", 3456789012u);
        AssertPropertyNotValue(deserialized, "SignedLong", -1234567890123456789L);
        AssertPropertyNotValue(deserialized, "UnsignedLong", 12345678901234567890UL);
        AssertPropertyNotValue(deserialized, "FloatValue", 1.50f);
        AssertPropertyNotValue(deserialized, "DoubleValue", 10.8d);
        AssertPropertyNotValue(deserialized, "Letter", 'Z');
        AssertPropertyNotValue(deserialized, "Name", "Alpha");
        AssertEnumPropertyNotValue(assembly, deserialized, "SmallState", "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done");
        AssertEnumPropertyNotValue(assembly, deserialized, "LargeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two");

        InvokeDeserialize(deserialized, serializedBytes);

        AssertPropertyValue(deserialized, "Flag", true);
        AssertPropertyValue(deserialized, "SignedByte", (sbyte)-12);
        AssertPropertyValue(deserialized, "UnsignedByte", (byte)200);
        AssertPropertyValue(deserialized, "SignedShort", (short)-1234);
        AssertPropertyValue(deserialized, "UnsignedShort", (ushort)54321);
        AssertPropertyValue(deserialized, "SignedInt", -123456789);
        AssertPropertyValue(deserialized, "UnsignedInt", 3456789012u);
        AssertPropertyValue(deserialized, "SignedLong", -1234567890123456789L);
        AssertPropertyValue(deserialized, "UnsignedLong", 12345678901234567890UL);
        AssertPropertyValue(deserialized, "FloatValue", 1.50f);
        AssertPropertyValue(deserialized, "DoubleValue", 10.8d);
        AssertPropertyValue(deserialized, "Letter", 'Z');
        AssertPropertyValue(deserialized, "Name", "Alpha");
        AssertEnumPropertyValue(assembly, deserialized, "SmallState", "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done");
        AssertEnumPropertyValue(assembly, deserialized, "LargeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two");

        Assert.True(GetProperty<bool>(deserialized, "IsDirty"));

        Invoke(deserialized, "ClearDirty");
        Assert.False(GetProperty<bool>(deserialized, "IsDirty"));

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, serializedBytes);
        Invoke(deltaReceiver, "ClearDirty");
        Invoke(instance, "ClearDirty");

        SetProperty(instance, "Flag", false);
        SetProperty(instance, "UnsignedInt", 4000000000u);
        SetProperty(instance, "FloatValue", 2.0f);
        SetProperty(instance, "Name", "Beta");
        SetProperty(instance, "LargeState", ParseEnum(assembly, "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "One"));

        var deltaBytes = InvokeWriteDelta(instance);
        var deltaReader = new NetDataReader(deltaBytes);
        var rawMask = deltaReader.GetUShort();

        const ushort expectedMask =
            (1 << 0) |
            (1 << 6) |
            (1 << 9) |
            (1 << 12) |
            (1 << 14);

        Assert.Equal(expectedMask, rawMask);

        AssertPropertyNotValue(deltaReceiver, "Flag", false);
        AssertPropertyNotValue(deltaReceiver, "UnsignedInt", 4000000000u);
        AssertPropertyNotValue(deltaReceiver, "FloatValue", 2.0f);
        AssertPropertyNotValue(deltaReceiver, "Name", "Beta");
        AssertEnumPropertyNotValue(assembly, deltaReceiver, "LargeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "One");

        InvokeReadDelta(deltaReceiver, deltaBytes);

        AssertPropertyValue(deltaReceiver, "Flag", false);
        AssertPropertyValue(deltaReceiver, "SignedByte", (sbyte)-12);
        AssertPropertyValue(deltaReceiver, "UnsignedByte", (byte)200);
        AssertPropertyValue(deltaReceiver, "SignedShort", (short)-1234);
        AssertPropertyValue(deltaReceiver, "UnsignedShort", (ushort)54321);
        AssertPropertyValue(deltaReceiver, "SignedInt", -123456789);
        AssertPropertyValue(deltaReceiver, "UnsignedInt", 4000000000u);
        AssertPropertyValue(deltaReceiver, "SignedLong", -1234567890123456789L);
        AssertPropertyValue(deltaReceiver, "UnsignedLong", 12345678901234567890UL);
        AssertPropertyValue(deltaReceiver, "FloatValue", 2.0f);
        AssertPropertyValue(deltaReceiver, "DoubleValue", 10.8d);
        AssertPropertyValue(deltaReceiver, "Letter", 'Z');
        AssertPropertyValue(deltaReceiver, "Name", "Beta");
        AssertEnumPropertyValue(assembly, deltaReceiver, "SmallState", "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done");
        AssertEnumPropertyValue(assembly, deltaReceiver, "LargeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "One");

        Assert.True(GetProperty<bool>(deltaReceiver, "IsDirty"));

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        InvokeDeserialize(skippedReceiver, serializedBytes);
        Invoke(skippedReceiver, "ClearDirty");

        AssertPropertyValue(skippedReceiver, "Flag", true);
        AssertPropertyValue(skippedReceiver, "UnsignedInt", 3456789012u);
        AssertPropertyValue(skippedReceiver, "FloatValue", 1.50f);
        AssertPropertyValue(skippedReceiver, "Name", "Alpha");
        AssertEnumPropertyValue(assembly, skippedReceiver, "LargeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two");

        InvokeSkipDelta(skippedReceiver, deltaBytes);

        AssertPropertyValue(skippedReceiver, "Flag", true);
        AssertPropertyValue(skippedReceiver, "SignedByte", (sbyte)-12);
        AssertPropertyValue(skippedReceiver, "UnsignedByte", (byte)200);
        AssertPropertyValue(skippedReceiver, "SignedShort", (short)-1234);
        AssertPropertyValue(skippedReceiver, "UnsignedShort", (ushort)54321);
        AssertPropertyValue(skippedReceiver, "SignedInt", -123456789);
        AssertPropertyValue(skippedReceiver, "UnsignedInt", 3456789012u);
        AssertPropertyValue(skippedReceiver, "SignedLong", -1234567890123456789L);
        AssertPropertyValue(skippedReceiver, "UnsignedLong", 12345678901234567890UL);
        AssertPropertyValue(skippedReceiver, "FloatValue", 1.50f);
        AssertPropertyValue(skippedReceiver, "DoubleValue", 10.8d);
        AssertPropertyValue(skippedReceiver, "Letter", 'Z');
        AssertPropertyValue(skippedReceiver, "Name", "Alpha");
        AssertEnumPropertyValue(assembly, skippedReceiver, "SmallState", "ReadyM.Api.Generators.Tests.TestTypes.SmallState", "Done");
        AssertEnumPropertyValue(assembly, skippedReceiver, "LargeState", "ReadyM.Api.Generators.Tests.TestTypes.LargeState", "Two");
    }

    [Fact]
    public void ComplexCoverageComponent_VectorAndCustomFields_BehaveAsExpected()
    {
        const string source = """
using System;
using System.Numerics;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct CustomValue : IEquatable<CustomValue>
{
    public int Id;
    public float Amount;

    public readonly bool DeltaEquals(CustomValue other, float epsilon)
        => Id == other.Id && Math.Abs(Amount - other.Amount) <= epsilon;

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

[DeriveINetworkedComponent]
public partial struct ComplexCoverageComponent : INetworkedComponent
{
    private Vector2 _position2;
    private Vector3 _position3;
    private CustomValue _payload;
    private int _revision;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.ComplexCoverageComponent");
        var customValueType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.CustomValue");

        Assert.NotNull(componentType);
        Assert.NotNull(customValueType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        SetProperty(instance, "Position2", new System.Numerics.Vector2(1.0f, 2.0f));
        SetProperty(instance, "Position3", new System.Numerics.Vector3(3.0f, 4.0f, 5.0f));
        SetProperty(instance, "Payload", CreateCustomValue(customValueType, id: 10, amount: 2.5f));
        SetProperty(instance, "Revision", 7);

        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Position2", new System.Numerics.Vector2(1.05f, 2.0f));
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Position3", new System.Numerics.Vector3(3.05f, 4.0f, 5.0f));
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Payload", CreateCustomValue(customValueType, id: 10, amount: 2.505f));
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Position2", new System.Numerics.Vector2(1.2f, 2.0f));
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Position3", new System.Numerics.Vector3(3.2f, 4.0f, 5.0f));
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Payload", CreateCustomValue(customValueType, id: 11, amount: 2.7f));
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        var serializedBytes = InvokeSerialize(instance);
        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        AssertPropertyNotValue(deserialized, "Position2", new System.Numerics.Vector2(1.2f, 2.0f));
        AssertPropertyNotValue(deserialized, "Position3", new System.Numerics.Vector3(3.2f, 4.0f, 5.0f));
        AssertCustomValueNotValue(customValueType, GetProperty<object>(deserialized, "Payload"), expectedId: 11, expectedAmount: 2.7f);
        AssertPropertyNotValue(deserialized, "Revision", 7);

        InvokeDeserialize(deserialized, serializedBytes);

        AssertPropertyValue(deserialized, "Position2", new System.Numerics.Vector2(1.2f, 2.0f));
        AssertPropertyValue(deserialized, "Position3", new System.Numerics.Vector3(3.2f, 4.0f, 5.0f));
        AssertCustomValueValue(customValueType, GetProperty<object>(deserialized, "Payload"), expectedId: 11, expectedAmount: 2.7f);
        AssertPropertyValue(deserialized, "Revision", 7);
        Assert.True(GetProperty<bool>(deserialized, "IsDirty"));

        Invoke(deserialized, "ClearDirty");
        Assert.False(GetProperty<bool>(deserialized, "IsDirty"));

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, serializedBytes);
        Invoke(deltaReceiver, "ClearDirty");
        Invoke(instance, "ClearDirty");

        SetProperty(instance, "Position2", new System.Numerics.Vector2(4.0f, 5.0f));
        SetProperty(instance, "Payload", CreateCustomValue(customValueType, id: 99, amount: 9.5f));

        var deltaBytes = InvokeWriteDelta(instance);
        var deltaReader = new NetDataReader(deltaBytes);
        var rawMask = deltaReader.GetByte();

        Assert.Equal((byte)((1 << 0) | (1 << 2)), rawMask);

        AssertPropertyNotValue(deltaReceiver, "Position2", new System.Numerics.Vector2(4.0f, 5.0f));
        AssertCustomValueNotValue(customValueType, GetProperty<object>(deltaReceiver, "Payload"), expectedId: 99, expectedAmount: 9.5f);

        InvokeReadDelta(deltaReceiver, deltaBytes);

        AssertPropertyValue(deltaReceiver, "Position2", new System.Numerics.Vector2(4.0f, 5.0f));
        AssertPropertyValue(deltaReceiver, "Position3", new System.Numerics.Vector3(3.2f, 4.0f, 5.0f));
        AssertCustomValueValue(customValueType, GetProperty<object>(deltaReceiver, "Payload"), expectedId: 99, expectedAmount: 9.5f);
        AssertPropertyValue(deltaReceiver, "Revision", 7);

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        InvokeDeserialize(skippedReceiver, serializedBytes);
        Invoke(skippedReceiver, "ClearDirty");

        AssertPropertyValue(skippedReceiver, "Position2", new System.Numerics.Vector2(1.2f, 2.0f));
        AssertCustomValueValue(customValueType, GetProperty<object>(skippedReceiver, "Payload"), expectedId: 11, expectedAmount: 2.7f);

        InvokeSkipDelta(skippedReceiver, deltaBytes);

        AssertPropertyValue(skippedReceiver, "Position2", new System.Numerics.Vector2(1.2f, 2.0f));
        AssertPropertyValue(skippedReceiver, "Position3", new System.Numerics.Vector3(3.2f, 4.0f, 5.0f));
        AssertCustomValueValue(customValueType, GetProperty<object>(skippedReceiver, "Payload"), expectedId: 11, expectedAmount: 2.7f);
        AssertPropertyValue(skippedReceiver, "Revision", 7);
    }

    [Theory]
    [InlineData(7, "byte")]
    [InlineData(8, "byte")]
    [InlineData(9, "ushort")]
    [InlineData(15, "ushort")]
    [InlineData(16, "ushort")]
    [InlineData(17, "uint")]
    [InlineData(31, "uint")]
    [InlineData(32, "uint")]
    [InlineData(33, "ulong")]
    [InlineData(63, "ulong")]
    [InlineData(64, "ulong")]
    public void DirtyMaskWidth_SwitchesAtExpectedFieldCounts(int fieldCount, string expectedMaskType)
    {
        var typeName = "Mask" + fieldCount + "Component";
        var source = GenerateMultiFieldMaskComponentSource(typeName, "int", fieldCount);

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("private " + expectedMaskType + " _dirtyMask;", generatedText);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes." + typeName);
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        for (var i = 0; i < fieldCount; i++)
        {
            SetProperty(instance, "Value" + i, 555 + i);
        }

        var serializedBytes = InvokeSerialize(instance);
        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        for (var i = 0; i < fieldCount; i++)
        {
            AssertPropertyNotValue(deserialized, "Value" + i, 555 + i);
        }

        InvokeDeserialize(deserialized, serializedBytes);

        for (var i = 0; i < fieldCount; i++)
        {
            AssertPropertyValue(deserialized, "Value" + i, 555 + i);
        }

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, serializedBytes);
        Invoke(deltaReceiver, "ClearDirty");
        Invoke(instance, "ClearDirty");
        
        var originalFirst = GetProperty<int>(instance, "Value0");
        var originalLast = GetProperty<int>(instance, "Value" + (fieldCount - 1));

        SetProperty(instance, "Value0", originalFirst + 100);
        SetProperty(instance, "Value" + (fieldCount - 1), originalLast + 200);

        var deltaBytes = InvokeWriteDelta(instance);
        AssertMaskValue(deltaBytes, (1UL << 0) | (1UL << (fieldCount - 1)), fieldCount);

        AssertPropertyNotValue(deltaReceiver, "Value0", originalFirst + 100);
        AssertPropertyNotValue(deltaReceiver, "Value" + (fieldCount - 1), originalLast + 200);

        InvokeReadDelta(deltaReceiver, deltaBytes);

        AssertPropertyValue(deltaReceiver, "Value0", originalFirst + 100);
        AssertPropertyValue(deltaReceiver, "Value" + (fieldCount - 1), originalLast + 200);

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        InvokeDeserialize(skippedReceiver, serializedBytes);
        Invoke(skippedReceiver, "ClearDirty");

        AssertPropertyValue(skippedReceiver, "Value0", originalFirst);
        AssertPropertyValue(skippedReceiver, "Value" + (fieldCount - 1), originalLast);

        InvokeSkipDelta(skippedReceiver, deltaBytes);

        AssertPropertyValue(skippedReceiver, "Value0", originalFirst);
        AssertPropertyValue(skippedReceiver, "Value" + (fieldCount - 1), originalLast);
    }

    [Fact]
    public void FloatFieldGeneratedMembersBehaveAsExpected()
    {
        const string source = """
using System;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct FloatComponent : INetworkedComponent
{
    private float _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.FloatComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        var valueProperty = componentType.GetProperty("Value");
        Assert.NotNull(valueProperty);

        var isDirtyProperty = componentType.GetProperty("IsDirty");
        Assert.NotNull(isDirtyProperty);

        var clearDirtyMethod = componentType.GetMethod("ClearDirty");
        Assert.NotNull(clearDirtyMethod);

        clearDirtyMethod.Invoke(instance, Array.Empty<object>());

        var initiallyDirty = (bool)isDirtyProperty.GetValue(instance)!;
        Assert.False(initiallyDirty);

        valueProperty.SetValue(instance, 0.05f);
        var stillCleanAfterSmallChange = (bool)isDirtyProperty.GetValue(instance)!;
        Assert.False(stillCleanAfterSmallChange);

        valueProperty.SetValue(instance, 0.2f);
        var dirtyAfterLargeChange = (bool)isDirtyProperty.GetValue(instance)!;
        Assert.True(dirtyAfterLargeChange);

        var storedValue = (float)valueProperty.GetValue(instance)!;
        Assert.Equal(0.2f, storedValue);

        clearDirtyMethod.Invoke(instance, Array.Empty<object>());
        var cleanAfterClear = (bool)isDirtyProperty.GetValue(instance)!;
        Assert.False(cleanAfterClear);

        valueProperty.SetValue(instance, 0.25f);
        var stillCleanAfterSecondSmallChange = (bool)isDirtyProperty.GetValue(instance)!;
        Assert.False(stillCleanAfterSecondSmallChange);

        valueProperty.SetValue(instance, 0.4f);
        var dirtyAfterSecondLargeChange = (bool)isDirtyProperty.GetValue(instance)!;
        Assert.True(dirtyAfterSecondLargeChange);
    }

    [Fact]
    public void ZeroFieldComponent_AllGeneratedMethods_BehaveAsExpected()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct ZeroFieldComponent : INetworkedComponent
{
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.ZeroFieldComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        var serializedBytes = InvokeSerialize(instance);
        Assert.Empty(serializedBytes);

        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        InvokeDeserialize(deserialized, serializedBytes);
        Assert.False(GetProperty<bool>(deserialized, "IsDirty"));

        var deltaBytes = InvokeWriteDelta(instance);
        Assert.Single(deltaBytes);

        var reader = new NetDataReader(deltaBytes);
        Assert.Equal((byte)0, reader.GetByte());
        Assert.True(reader.EndOfData);

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeReadDelta(deltaReceiver, deltaBytes);
        Assert.False(GetProperty<bool>(deltaReceiver, "IsDirty"));

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        InvokeSkipDelta(skippedReceiver, deltaBytes);
        Assert.False(GetProperty<bool>(skippedReceiver, "IsDirty"));
    }
    
    [Fact]
    public void SingleFieldComponent_AllGeneratedMethods_BehaveAsExpected()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct SingleFieldComponent : INetworkedComponent
{
    private int _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.SingleFieldComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        SetProperty(instance, "Value", 123);
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        AssertPropertyNotValue(deserialized, "Value", 123);
        InvokeDeserialize(deserialized, serializedBytes);
        AssertPropertyValue(deserialized, "Value", 123);
        Assert.True(GetProperty<bool>(deserialized, "IsDirty"));

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, serializedBytes);
        Invoke(deltaReceiver, "ClearDirty");
        Invoke(instance, "ClearDirty");

        SetProperty(instance, "Value", 456);

        var deltaBytes = InvokeWriteDelta(instance);
        AssertMaskValue8(deltaBytes, (byte)1);

        AssertPropertyNotValue(deltaReceiver, "Value", 456);
        InvokeReadDelta(deltaReceiver, deltaBytes);
        AssertPropertyValue(deltaReceiver, "Value", 456);

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        InvokeDeserialize(skippedReceiver, serializedBytes);
        Invoke(skippedReceiver, "ClearDirty");

        AssertPropertyValue(skippedReceiver, "Value", 123);
        InvokeSkipDelta(skippedReceiver, deltaBytes);
        AssertPropertyValue(skippedReceiver, "Value", 123);
    }
    
    [Fact]
    public void TwoFieldComponent_PartialDeltas_MergeIntoZeroedTargetAsExpected()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct TwoFieldComponent : INetworkedComponent
{
    private int _left;
    private int _right;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.TwoFieldComponent");
        Assert.NotNull(componentType);

        var baseline = Activator.CreateInstance(componentType);
        Assert.NotNull(baseline);

        SetProperty(baseline, "Left", 100);
        SetProperty(baseline, "Right", 200);

        var baselineBytes = InvokeSerialize(baseline);

        var zeroedTarget = Activator.CreateInstance(componentType);
        Assert.NotNull(zeroedTarget);

        AssertPropertyValue(zeroedTarget, "Left", 0);
        AssertPropertyValue(zeroedTarget, "Right", 0);

        var source1 = Activator.CreateInstance(componentType);
        Assert.NotNull(source1);
        InvokeDeserialize(source1, baselineBytes);
        Invoke(source1, "ClearDirty");
        SetProperty(source1, "Left", 111);

        var delta1 = InvokeWriteDelta(source1);
        AssertMaskValue8(delta1, (byte)(1 << 0));

        InvokeReadDelta(zeroedTarget, delta1);
        AssertPropertyValue(zeroedTarget, "Left", 111);
        AssertPropertyValue(zeroedTarget, "Right", 0);

        var source2 = Activator.CreateInstance(componentType);
        Assert.NotNull(source2);
        InvokeDeserialize(source2, baselineBytes);
        Invoke(source2, "ClearDirty");
        SetProperty(source2, "Right", 222);

        var delta2 = InvokeWriteDelta(source2);
        AssertMaskValue8(delta2, (byte)(1 << 1));

        InvokeReadDelta(zeroedTarget, delta2);
        AssertPropertyValue(zeroedTarget, "Left", 111);
        AssertPropertyValue(zeroedTarget, "Right", 222);

        var untouchedTarget = Activator.CreateInstance(componentType);
        Assert.NotNull(untouchedTarget);

        AssertPropertyValue(untouchedTarget, "Left", 0);
        AssertPropertyValue(untouchedTarget, "Right", 0);

        InvokeSkipDelta(untouchedTarget, delta1);
        AssertPropertyValue(untouchedTarget, "Left", 0);
        AssertPropertyValue(untouchedTarget, "Right", 0);
    }
    
    [Fact]
    public void MaxSupportedFieldCount_ComponentWithSixtyFourFields_BehavesAsExpected()
    {
        const int fieldCount = 64;
        var typeName = "MaxFieldCountComponent";
        var source = GenerateMultiFieldMaskComponentSource(typeName, "int", fieldCount);

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("private ulong _dirtyMask;", generatedText);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes." + typeName);
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        for (var i = 0; i < fieldCount; i++)
        {
            SetProperty(instance, "Value" + i, 1000 + i);
        }

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        InvokeDeserialize(deserialized, serializedBytes);

        AssertPropertyValue(deserialized, "Value0", 1000);
        AssertPropertyValue(deserialized, "Value63", 1063);

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, serializedBytes);
        Invoke(deltaReceiver, "ClearDirty");
        Invoke(instance, "ClearDirty");

        SetProperty(instance, "Value0", 5000);
        SetProperty(instance, "Value63", 9000);

        var deltaBytes = InvokeWriteDelta(instance);
        AssertMaskValue64(deltaBytes, (1UL << 0) | (1UL << 63));

        InvokeReadDelta(deltaReceiver, deltaBytes);

        AssertPropertyValue(deltaReceiver, "Value0", 5000);
        AssertPropertyValue(deltaReceiver, "Value63", 9000);

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        InvokeDeserialize(skippedReceiver, serializedBytes);
        Invoke(skippedReceiver, "ClearDirty");

        InvokeSkipDelta(skippedReceiver, deltaBytes);

        AssertPropertyValue(skippedReceiver, "Value0", 1000);
        AssertPropertyValue(skippedReceiver, "Value63", 1063);
    }
    
    [Fact]
    public void StringField_NullTransitions_AndDeltaBehaveAsExpected()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct NullableStringComponent : INetworkedComponent
{
    private string? _name;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.NullableStringComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        AssertNullPropertyValue(instance, "Name");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Name", null);
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Name", "Alpha");
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Name", "Alpha");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Name", null);
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        AssertNullPropertyValue(deserialized, "Name");
        InvokeDeserialize(deserialized, serializedBytes);
        
        // NOTE: This is the current behavior as string null values are treated the same as empty strings
        // This test is checking that this behavior doesn't change unintentionally
        // Feel free to update the test to reflect the changes if this behavior is changed in the future
        AssertNotNullPropertyValue(deserialized, "Name");

        var baseline = Activator.CreateInstance(componentType);
        Assert.NotNull(baseline);

        SetProperty(baseline, "Name", "Start");
        var baselineBytes = InvokeSerialize(baseline);

        var deltaSource = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaSource);

        InvokeDeserialize(deltaSource, baselineBytes);
        Invoke(deltaSource, "ClearDirty");
        SetProperty(deltaSource, "Name", null);

        var deltaBytes = InvokeWriteDelta(deltaSource);
        AssertMaskValue8(deltaBytes, (byte)1);

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, baselineBytes);
        AssertNotNullPropertyValue(deltaReceiver, "Name");
        InvokeReadDelta(deltaReceiver, deltaBytes);
        
        AssertNotNullPropertyValue(deltaReceiver, "Name");
        AssertPropertyValue(deltaReceiver, "Name", "");

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        InvokeDeserialize(skippedReceiver, baselineBytes);
        InvokeSkipDelta(skippedReceiver, deltaBytes);
        AssertPropertyValue(skippedReceiver, "Name", "Start");
    }
    
    [Fact]
    public void ZeroDirtyMask_OnPopulatedComponent_WriteReadAndSkipDeltaBehaveAsExpected()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct ZeroDeltaComponent : INetworkedComponent
{
    private int _value;
    private string? _name;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.ZeroDeltaComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        SetProperty(instance, "Value", 321);
        SetProperty(instance, "Name", "Ready");
        Invoke(instance, "ClearDirty");

        var deltaBytes = InvokeWriteDelta(instance);
        AssertMaskValue8(deltaBytes, (byte)0);

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        SetProperty(deltaReceiver, "Value", 999);
        SetProperty(deltaReceiver, "Name", "KeepMe");

        InvokeReadDelta(deltaReceiver, deltaBytes);

        AssertPropertyValue(deltaReceiver, "Value", 999);
        AssertPropertyValue(deltaReceiver, "Name", "KeepMe");

        var skippedReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(skippedReceiver);

        SetProperty(skippedReceiver, "Value", 888);
        SetProperty(skippedReceiver, "Name", "AlsoKeep");

        InvokeSkipDelta(skippedReceiver, deltaBytes);

        AssertPropertyValue(skippedReceiver, "Value", 888);
        AssertPropertyValue(skippedReceiver, "Name", "AlsoKeep");
    }
    
    [Fact]
    public void DeltaEquatableField_SmallAndLargeChanges_BehaveAsExpected()
    {
        const string source = """
using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct DeltaOnlyValue : IDeltaEquatable<DeltaOnlyValue>
{
    public float Amount;

    public readonly bool DeltaEquals(DeltaOnlyValue other, float epsilon)
        => Math.Abs(Amount - other.Amount) <= epsilon;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Amount);
    }

    public void Deserialize(NetDataReader reader)
    {
        Amount = reader.GetFloat();
    }
}

[DeriveINetworkedComponent]
public partial struct DeltaEquatableComponent : INetworkedComponent
{
    private DeltaOnlyValue _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.DeltaEquatableComponent");
        var valueType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.DeltaOnlyValue");

        Assert.NotNull(componentType);
        Assert.NotNull(valueType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        SetProperty(instance, "Value", CreateSingleFloatFieldStruct(valueType, "Amount", 1.00f));
        Invoke(instance, "ClearDirty");

        SetProperty(instance, "Value", CreateSingleFloatFieldStruct(valueType, "Amount", 1.005f));
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Value", CreateSingleFloatFieldStruct(valueType, "Amount", 1.20f));
        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        AssertSingleFloatFieldStructNotValue(valueType, GetProperty<object>(deserialized, "Value"), "Amount", 1.20f);
        InvokeDeserialize(deserialized, serializedBytes);
        AssertSingleFloatFieldStructValue(valueType, GetProperty<object>(deserialized, "Value"), "Amount", 1.20f);
    }
    
    [Fact]
    public void EquatableField_UsesEqualsForDirtyTracking()
    {
        const string source = """
using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct EquatableOnlyValue : IEquatable<EquatableOnlyValue>
{
    public int Id;

    public readonly bool Equals(EquatableOnlyValue other)
        => Id == other.Id;

    public override readonly bool Equals(object? obj)
        => obj is EquatableOnlyValue other && Equals(other);

    public override readonly int GetHashCode()
        => Id;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
    }
}

[DeriveINetworkedComponent]
public partial struct EquatableOnlyComponent : INetworkedComponent
{
    private EquatableOnlyValue _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.EquatableOnlyComponent");
        var valueType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.EquatableOnlyValue");

        Assert.NotNull(componentType);
        Assert.NotNull(valueType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        SetProperty(instance, "Value", CreateSingleIntFieldStruct(valueType, "Id", 10));
        Invoke(instance, "ClearDirty");

        SetProperty(instance, "Value", CreateSingleIntFieldStruct(valueType, "Id", 10));
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Value", CreateSingleIntFieldStruct(valueType, "Id", 11));
        Assert.True(GetProperty<bool>(instance, "IsDirty"));
    }

    [Fact]
    public void NonPrivateFieldMappingModes_IncludeConfiguredAccessLevelsOnly()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(mode: SerializableMode.MapFields | SerializableMode.MapPublic | SerializableMode.MapInternal)]
public partial struct NonPrivateMappedFieldsComponent : INetworkedComponent
{
    private int _privateValue;
    public int _publicValue;
    internal int _internalValue;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.DoesNotContain("PrivateValue", generatedText);
        Assert.Contains("PublicValue", generatedText);
        Assert.Contains("InternalValue", generatedText);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.NonPrivateMappedFieldsComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        Assert.Null(componentType.GetProperty("PrivateValue"));
        Assert.NotNull(componentType.GetProperty("PublicValue"));
        Assert.NotNull(componentType.GetProperty("InternalValue"));

        SetProperty(instance, "PublicValue", 10);
        SetProperty(instance, "InternalValue", 20);

        var bytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        InvokeDeserialize(deserialized, bytes);

        AssertPropertyValue(deserialized, "PublicValue", 10);
        AssertPropertyValue(deserialized, "InternalValue", 20);
    }
    
    [Fact]
    public void PropertyMappingMode_AutoProperties_AreSerializedAndDeltaApplied()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(mode: SerializableMode.MapProperties | SerializableMode.MapPublic | SerializableMode.MapInternal)]
public partial struct PropertyMappedComponent : INetworkedComponent
{
    public int PublicValue { get; set; }
    internal int InternalValue { get; set; }
    private int PrivateValue { get; set; }
    public int _publicOtherValue { get; set; }
    internal int internalOtherValue { get; set; }
    private int privateOtherValue { get; set; }
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("PublicValueDirtyAware", generatedText);
        Assert.Contains("InternalValueDirtyAware", generatedText);
        Assert.Contains("InternalOtherValue", generatedText);
        Assert.Contains("InternalOtherValue", generatedText);
        Assert.DoesNotContain("PrivateValue", generatedText);
        Assert.DoesNotContain("PrivateOtherValue", generatedText);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.PropertyMappedComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType);
        Assert.NotNull(instance);

        SetProperty(instance, "PublicValueDirtyAware", 100);
        SetProperty(instance, "InternalValueDirtyAware", 200);
        SetProperty(instance, "PublicOtherValue", 300);
        SetProperty(instance, "InternalOtherValue", 400);

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(componentType);
        Assert.NotNull(deserialized);

        InvokeDeserialize(deserialized, serializedBytes);

        AssertPropertyValue(deserialized, "PublicValueDirtyAware", 100);
        AssertPropertyValue(deserialized, "InternalValueDirtyAware", 200);
        AssertPropertyValue(deserialized, "PublicOtherValue", 300);
        AssertPropertyValue(deserialized, "InternalOtherValue", 400);

        var deltaReceiver = Activator.CreateInstance(componentType);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, serializedBytes);
        Invoke(deltaReceiver, "ClearDirty");
        Invoke(instance, "ClearDirty");

        SetProperty(instance, "InternalValueDirtyAware", 333);

        var deltaBytes = InvokeWriteDelta(instance);
        InvokeReadDelta(deltaReceiver, deltaBytes);

        AssertPropertyValue(deltaReceiver, "PublicValueDirtyAware", 100);
        AssertPropertyValue(deltaReceiver, "InternalValueDirtyAware", 333);
        AssertPropertyValue(deltaReceiver, "PublicOtherValue", 300);
        AssertPropertyValue(deltaReceiver, "InternalOtherValue", 400);
    }
    
    [Fact]
    public void EmitDirtyMaskFalse_DirtyMaskIsNotMappedAndGeneratedShapeMatchesExpectation()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(emitDirtyMask: false, mode: SerializableMode.MapFields | SerializableMode.MapPrivate | SerializableMode.MapPublic)]
public partial struct EmitDirtyMaskFalseComponent : INetworkedComponent
{
    private byte _dirtyMask;

    private int _value;
    public int _otherValue;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);
        
        Assert.DoesNotContain("byte _dirtyMask;", generatedText);
        Assert.DoesNotContain("ushort _dirtyMask;", generatedText);
        Assert.DoesNotContain("uint _dirtyMask;", generatedText);
        Assert.DoesNotContain("ulong _dirtyMask;", generatedText);
        Assert.DoesNotContain("byte DirtyMask", generatedText);
        Assert.DoesNotContain("ushort DirtyMask", generatedText);
        Assert.DoesNotContain("uint DirtyMask", generatedText);
        Assert.DoesNotContain("ulong DirtyMask", generatedText);
    }
    
    [Fact]
    public void EmitDirtyMaskTrue_DirtyMaskIsNotMappedAndGeneratedShapeMatchesExpectation()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(emitDirtyMask: true, mode: SerializableMode.MapFields | SerializableMode.MapPrivate | SerializableMode.MapPublic)]
public partial struct EmitDirtyMaskTrueComponent : INetworkedComponent
{
    private int _value;
    public int _otherValue;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);
        
        Assert.Contains("byte _dirtyMask;", generatedText);
        Assert.DoesNotContain("byte DirtyMask", generatedText);
        Assert.DoesNotContain("ushort DirtyMask", generatedText);
        Assert.DoesNotContain("uint DirtyMask", generatedText);
        Assert.DoesNotContain("ulong DirtyMask", generatedText);
    }
    
    [Fact]
    public void GeneratedShape_ForRepresentativeComponent_UsesExpectedMaskAndReaderWriterMethods()
    {
        const string source = """
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using LiteNetLib.Utils;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum TinyState : byte
{
    A = 0,
    B = 1
}

[DeriveINetworkedComponent]
public partial struct GeneratedShapeComponent : INetworkedComponent
{
    private int _count;
    private string? _name;
    private TinyState _state;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("private byte _dirtyMask;", generatedText);
        Assert.Contains("writer.Put(_count);", generatedText);
        Assert.Contains("writer.Put(_name);", generatedText);
        Assert.Contains("writer.Put((byte)_state);", generatedText);
        Assert.Contains("= reader.GetInt();", generatedText);
        Assert.Contains("Count =", generatedText);
        Assert.Contains("= reader.GetString();", generatedText);
        Assert.Contains("Name =", generatedText);
        Assert.Contains("= (global::ReadyM.Api.Generators.Tests.TestTypes.TinyState)reader.GetByte();", generatedText);
        Assert.Contains("State =", generatedText);
        Assert.Contains("var mask = reader.GetByte();", generatedText);
    }
    
    [Fact]
    public void NativeContainerCoverageComponent_NativeStringsAndNativeDictionaries_BehaveAsExpected()
    {
        const string source = """
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct NativeContainerCoverageComponent(AllocatorKind kind) : INetworkedComponent
{
    private NativeString256 _name256;
    private NativeString64 _name64;

    private NativeDictionary<NativeString256, float, NativeStringHash256> _string256ToFloat = new(8, kind);
    private NativeDictionary<int, NativeString256, IntHash> _intToString256 = new(4, kind);
    private NativeDictionary<NativeString256, NativeString64, NativeStringHash256> _string256ToString64 = new(2, kind);
    private NativeDictionary<int, double, IntHash> _intToDouble = new(0, kind);
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.NativeContainerCoverageComponent");
        Assert.NotNull(componentType);

        var instance = Activator.CreateInstance(componentType, AllocatorKind.Marshal);
        Assert.NotNull(instance);

        var name256Type = componentType.GetProperty("Name256")!.PropertyType;
        var name64Type = componentType.GetProperty("Name64")!.PropertyType;

        var string256ToFloatType = componentType.GetProperty("String256ToFloat")!.PropertyType;
        var intToString256Type = componentType.GetProperty("IntToString256")!.PropertyType;
        var string256ToString64Type = componentType.GetProperty("String256ToString64")!.PropertyType;
        var intToDoubleType = componentType.GetProperty("IntToDouble")!.PropertyType;

        var alpha256 = CreateNativeString(name256Type, "Alpha");
        var beta256 = CreateNativeString(name256Type, "Beta");
        var gamma256 = CreateNativeString(name256Type, "Gamma");
        var one256 = CreateNativeString(name256Type, "One");
        var two256 = CreateNativeString(name256Type, "Two");
        var three256 = CreateNativeString(name256Type, "Three");

        var shortA64 = CreateNativeString(name64Type, "ShortA");
        var shortB64 = CreateNativeString(name64Type, "ShortB");
        var shortC64 = CreateNativeString(name64Type, "ShortC");

        var string256ToFloat = CreateNativeDictionary(
            string256ToFloatType,
            (alpha256, 1.25f),
            (beta256, 2.50f));

        var intToString256 = CreateNativeDictionary(
            intToString256Type,
            (1, one256),
            (2, two256));

        var string256ToString64 = CreateNativeDictionary(
            string256ToString64Type,
            (alpha256, shortA64),
            (beta256, shortB64));

        var intToDouble = CreateNativeDictionary(
            intToDoubleType,
            (7, 10.5d),
            (9, 20.25d));

        SetProperty(instance, "Name256", alpha256);
        SetProperty(instance, "Name64", shortA64);
        SetProperty(instance, "String256ToFloat", string256ToFloat);
        SetProperty(instance, "IntToString256", intToString256);
        SetProperty(instance, "String256ToString64", string256ToString64);
        SetProperty(instance, "IntToDouble", intToDouble);

        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        Invoke(instance, "ClearDirty");
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        SetProperty(instance, "Name256", CreateNativeString(name256Type, "Alpha"));
        SetProperty(instance, "Name64", CreateNativeString(name64Type, "ShortA"));
        Assert.False(GetProperty<bool>(instance, "IsDirty"));

        var changedString256ToFloat = CreateNativeDictionary(
            string256ToFloatType,
            (alpha256, 1.25f),
            (beta256, 3.75f),
            (gamma256, 9.5f));

        var changedIntToString256 = CreateNativeDictionary(
            intToString256Type,
            (1, one256),
            (2, two256),
            (3, three256));

        var changedString256ToString64 = CreateNativeDictionary(
            string256ToString64Type,
            (alpha256, shortA64),
            (beta256, shortC64));

        var changedIntToDouble = CreateNativeDictionary(
            intToDoubleType,
            (7, 10.5d),
            (9, 21.75d));

        SetProperty(instance, "Name64", shortB64);
        SetProperty(instance, "String256ToFloat", changedString256ToFloat);
        SetProperty(instance, "IntToString256", changedIntToString256);
        SetProperty(instance, "String256ToString64", changedString256ToString64);
        SetProperty(instance, "IntToDouble", changedIntToDouble);

        Assert.True(GetProperty<bool>(instance, "IsDirty"));

        var serializedBytes = InvokeSerialize(instance);

        var deserialized = Activator.CreateInstance(componentType, AllocatorKind.Marshal);
        Assert.NotNull(deserialized);
        
        InvokeDeserialize(deserialized, serializedBytes);

        AssertNativeStringValue(GetProperty<object>(deserialized, "Name256"), "Alpha");
        AssertNativeStringValue(GetProperty<object>(deserialized, "Name64"), "ShortB");

        var deserializedString256ToFloat = GetProperty<object>(deserialized, "String256ToFloat");
        AssertNativeDictionaryCount(deserializedString256ToFloat, 3);
        AssertNativeDictionaryValue(deserializedString256ToFloat, alpha256, 1.25f);
        AssertNativeDictionaryValue(deserializedString256ToFloat, beta256, 3.75f);
        AssertNativeDictionaryValue(deserializedString256ToFloat, gamma256, 9.5f);

        var deserializedIntToString256 = GetProperty<object>(deserialized, "IntToString256");
        AssertNativeDictionaryCount(deserializedIntToString256, 3);
        AssertNativeDictionaryValue(deserializedIntToString256, 1, "One");
        AssertNativeDictionaryValue(deserializedIntToString256, 2, "Two");
        AssertNativeDictionaryValue(deserializedIntToString256, 3, "Three");

        var deserializedString256ToString64 = GetProperty<object>(deserialized, "String256ToString64");
        AssertNativeDictionaryCount(deserializedString256ToString64, 2);
        AssertNativeDictionaryValue(deserializedString256ToString64, alpha256, "ShortA");
        AssertNativeDictionaryValue(deserializedString256ToString64, beta256, "ShortC");

        var deserializedIntToDouble = GetProperty<object>(deserialized, "IntToDouble");
        AssertNativeDictionaryCount(deserializedIntToDouble, 2);
        AssertNativeDictionaryValue(deserializedIntToDouble, 7, 10.5d);
        AssertNativeDictionaryValue(deserializedIntToDouble, 9, 21.75d);

        Assert.True(GetProperty<bool>(deserialized, "IsDirty"));

        Invoke(deserialized, "ClearDirty");
        Assert.False(GetProperty<bool>(deserialized, "IsDirty"));

        var baseline = Activator.CreateInstance(componentType, AllocatorKind.Marshal);
        Assert.NotNull(baseline);

        var baselineString256ToFloat = CreateNativeDictionary(
            string256ToFloatType,
            (alpha256, 1.25f),
            (beta256, 2.50f));

        var baselineIntToString256 = CreateNativeDictionary(
            intToString256Type,
            (1, one256),
            (2, two256));

        var baselineString256ToString64 = CreateNativeDictionary(
            string256ToString64Type,
            (alpha256, shortA64),
            (beta256, shortB64));

        var baselineIntToDouble = CreateNativeDictionary(
            intToDoubleType,
            (7, 10.5d),
            (9, 20.25d));

        SetProperty(baseline, "Name256", alpha256);
        SetProperty(baseline, "Name64", shortA64);
        SetProperty(baseline, "String256ToFloat", baselineString256ToFloat);
        SetProperty(baseline, "IntToString256", baselineIntToString256);
        SetProperty(baseline, "String256ToString64", baselineString256ToString64);
        SetProperty(baseline, "IntToDouble", baselineIntToDouble);

        var baselineBytes = InvokeSerialize(baseline);

        var deltaSource = Activator.CreateInstance(componentType, AllocatorKind.Marshal);
        Assert.NotNull(deltaSource);

        InvokeDeserialize(deltaSource, baselineBytes);
        Invoke(deltaSource, "ClearDirty");

        SetProperty(deltaSource, "Name64", shortB64);
        SetProperty(deltaSource, "String256ToFloat", changedString256ToFloat);
        SetProperty(deltaSource, "IntToString256", changedIntToString256);
        SetProperty(deltaSource, "String256ToString64", changedString256ToString64);
        SetProperty(deltaSource, "IntToDouble", changedIntToDouble);

        var deltaBytes = InvokeWriteDelta(deltaSource);
        var deltaReader = new NetDataReader(deltaBytes);
        var rawMask = deltaReader.GetByte();

        const byte expectedMask =
            (1 << 1) |
            (1 << 2) |
            (1 << 3) |
            (1 << 4) |
            (1 << 5);

        Assert.Equal(expectedMask, rawMask);

        var deltaReceiver = Activator.CreateInstance(componentType, AllocatorKind.Marshal);
        Assert.NotNull(deltaReceiver);

        InvokeDeserialize(deltaReceiver, baselineBytes);
        Invoke(deltaReceiver, "ClearDirty");

        InvokeReadDelta(deltaReceiver, deltaBytes);

        AssertNativeStringValue(GetProperty<object>(deltaReceiver, "Name256"), "Alpha");
        AssertNativeStringValue(GetProperty<object>(deltaReceiver, "Name64"), "ShortB");

        var deltaString256ToFloat = GetProperty<object>(deltaReceiver, "String256ToFloat");
        AssertNativeDictionaryCount(deltaString256ToFloat, 3);
        AssertNativeDictionaryValue(deltaString256ToFloat, alpha256, 1.25f);
        AssertNativeDictionaryValue(deltaString256ToFloat, beta256, 3.75f);
        AssertNativeDictionaryValue(deltaString256ToFloat, gamma256, 9.5f);

        var deltaIntToString256 = GetProperty<object>(deltaReceiver, "IntToString256");
        AssertNativeDictionaryCount(deltaIntToString256, 3);
        AssertNativeDictionaryValue(deltaIntToString256, 1, "One");
        AssertNativeDictionaryValue(deltaIntToString256, 2, "Two");
        AssertNativeDictionaryValue(deltaIntToString256, 3, "Three");

        var deltaString256ToString64 = GetProperty<object>(deltaReceiver, "String256ToString64");
        AssertNativeDictionaryCount(deltaString256ToString64, 2);
        AssertNativeDictionaryValue(deltaString256ToString64, alpha256, "ShortA");
        AssertNativeDictionaryValue(deltaString256ToString64, beta256, "ShortC");

        var deltaIntToDouble = GetProperty<object>(deltaReceiver, "IntToDouble");
        AssertNativeDictionaryCount(deltaIntToDouble, 2);
        AssertNativeDictionaryValue(deltaIntToDouble, 7, 10.5d);
        AssertNativeDictionaryValue(deltaIntToDouble, 9, 21.75d);

        Assert.True(GetProperty<bool>(deltaReceiver, "IsDirty"));

        var skippedReceiver = Activator.CreateInstance(componentType, AllocatorKind.Marshal);
        Assert.NotNull(skippedReceiver);

        InvokeDeserialize(skippedReceiver, baselineBytes);
        Invoke(skippedReceiver, "ClearDirty");

        InvokeSkipDelta(skippedReceiver, deltaBytes);

        AssertNativeStringValue(GetProperty<object>(skippedReceiver, "Name256"), "Alpha");
        AssertNativeStringValue(GetProperty<object>(skippedReceiver, "Name64"), "ShortA");

        var skippedString256ToFloat = GetProperty<object>(skippedReceiver, "String256ToFloat");
        AssertNativeDictionaryCount(skippedString256ToFloat, 2);
        AssertNativeDictionaryValue(skippedString256ToFloat, alpha256, 1.25f);
        AssertNativeDictionaryValue(skippedString256ToFloat, beta256, 2.50f);

        var skippedIntToString256 = GetProperty<object>(skippedReceiver, "IntToString256");
        AssertNativeDictionaryCount(skippedIntToString256, 2);
        AssertNativeDictionaryValue(skippedIntToString256, 1, "One");
        AssertNativeDictionaryValue(skippedIntToString256, 2, "Two");

        var skippedString256ToString64 = GetProperty<object>(skippedReceiver, "String256ToString64");
        AssertNativeDictionaryCount(skippedString256ToString64, 2);
        AssertNativeDictionaryValue(skippedString256ToString64, alpha256, "ShortA");
        AssertNativeDictionaryValue(skippedString256ToString64, beta256, "ShortB");

        var skippedIntToDouble = GetProperty<object>(skippedReceiver, "IntToDouble");
        AssertNativeDictionaryCount(skippedIntToDouble, 2);
        AssertNativeDictionaryValue(skippedIntToDouble, 7, 10.5d);
        AssertNativeDictionaryValue(skippedIntToDouble, 9, 20.25d);
    }

    private static object CreateNativeString(Type nativeStringType, string value)
    {
        var ctor = nativeStringType.GetConstructor([typeof(string)]);
        Assert.NotNull(ctor);
        return ctor.Invoke([value]);
    }

    private static object CreateNativeDictionary(Type dictionaryType, params (object Key, object Value)[] items)
    {
        var ctor = dictionaryType.GetConstructors()
            .Single(c => c.GetParameters().Length == 2);

        var ctorParameters = ctor.GetParameters();
        var allocatorKindType = ctorParameters[1].ParameterType;
        var allocatorValue = Enum.ToObject(allocatorKindType, AllocatorKind.Marshal);

        var dictionary = ctor.Invoke([items.Length, allocatorValue]);

        var addMethod = dictionaryType.GetMethods()
            .Single(m =>
                m.Name == "Add" &&
                m.GetParameters().Length == 2);

        foreach (var (key, value) in items)
        {
            var added = (bool)addMethod.Invoke(dictionary, [key, value])!;
            Assert.True(added);
        }

        return dictionary;
    }

    private static void AssertNativeDictionaryCount(object dictionary, int expectedCount)
    {
        var countProperty = dictionary.GetType().GetProperty("Count");
        Assert.NotNull(countProperty);
        Assert.Equal(expectedCount, (int)countProperty.GetValue(dictionary)!);
    }

    private static void AssertNativeDictionaryValue(object dictionary, object key, object expectedValue)
    {
        var indexer = dictionary.GetType().GetProperty("Item");
        Assert.NotNull(indexer);

        var actualValue = indexer.GetValue(dictionary, [key]);
        Assert.NotNull(actualValue);

        AssertLooseValueEqual(actualValue, expectedValue);
    }

    private static void AssertNativeStringValue(object value, string expected)
    {
        AssertLooseValueEqual(value, expected);
    }

    private static void AssertLooseValueEqual(object? actual, object expected)
    {
        if (actual is null)
        {
            Assert.Null(expected);
            return;
        }

        if (expected is string expectedString &&
            actual.GetType().FullName is { } fullName &&
            fullName.StartsWith("Yooni.Native.Container.NativeString", StringComparison.Ordinal))
        {
            Assert.Equal(expectedString, actual.ToString());
            return;
        }

        if (expected is float expectedFloat)
        {
            Assert.Equal(expectedFloat, (float)actual);
            return;
        }

        if (expected is double expectedDouble)
        {
            Assert.Equal(expectedDouble, (double)actual);
            return;
        }

        Assert.Equal(expected, actual);
    }
    
    // ---
    
    private static object CreateSingleFloatFieldStruct(Type valueType, string fieldName, float value)
    {
        var instance = Activator.CreateInstance(valueType);
        Assert.NotNull(instance);

        var field = valueType.GetField(fieldName);
        Assert.NotNull(field);

        field.SetValue(instance, value);
        return instance;
    }

    private static object CreateSingleIntFieldStruct(Type valueType, string fieldName, int value)
    {
        var instance = Activator.CreateInstance(valueType);
        Assert.NotNull(instance);

        var field = valueType.GetField(fieldName);
        Assert.NotNull(field);

        field.SetValue(instance, value);
        return instance;
    }
    
    private static string GenerateMultiFieldMaskComponentSource(string typeName, string fieldName, int fieldCount)
    {
        var fields = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, fieldCount).Select(i => $"    private {fieldName} _value{i};"));

        return $$"""
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct {{typeName}} : INetworkedComponent
{
{{fields}}
}
""";
    }
}