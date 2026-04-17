using System.Reflection;
using LiteNetLib.Utils;
using Xunit;

namespace ReadyM.Api.Generators.Tests;

public static class DeriveTestAssert
{
    public static void AssertPropertyValue<T>(object instance, string propertyName, T expectedValue)
    {
        Assert.Equal(expectedValue, GetProperty<T>(instance, propertyName));
    }

    public static void AssertPropertyNotValue<T>(object instance, string propertyName, T unexpectedValue)
    {
        Assert.NotEqual(unexpectedValue, GetProperty<T>(instance, propertyName));
    }

    public static void AssertEnumPropertyValue(Assembly assembly, object instance, string propertyName, string enumTypeName, string expectedValueName)
    {
        var actualValue = GetProperty<object>(instance, propertyName);
        var expectedValue = ParseEnum(assembly, enumTypeName, expectedValueName);

        Assert.Equal(expectedValue, actualValue);
    }

    public static void AssertEnumPropertyNotValue(Assembly assembly, object instance, string propertyName, string enumTypeName, string unexpectedValueName)
    {
        var actualValue = GetProperty<object>(instance, propertyName);
        var unexpectedValue = ParseEnum(assembly, enumTypeName, unexpectedValueName);

        Assert.NotEqual(unexpectedValue, actualValue);
    }

    public static void AssertNullPropertyValue(object instance, string propertyName)
    {
        Assert.Null(GetProperty<object?>(instance, propertyName));
    }

    public static void AssertNotNullPropertyValue(object instance, string propertyName)
    {
        Assert.NotNull(GetProperty<object?>(instance, propertyName));
    }

    public static void AssertCustomValueValue(Type customValueType, object boxedValue, int expectedId, float expectedAmount)
    {
        var idField = customValueType.GetField("Id");
        var amountField = customValueType.GetField("Amount");

        Assert.NotNull(idField);
        Assert.NotNull(amountField);

        Assert.Equal(expectedId, (int)idField.GetValue(boxedValue)!);
        Assert.Equal(expectedAmount, (float)amountField.GetValue(boxedValue)!);
    }

    public static void AssertCustomValueNotValue(Type customValueType, object boxedValue, int expectedId, float expectedAmount)
    {
        var idField = customValueType.GetField("Id");
        var amountField = customValueType.GetField("Amount");

        Assert.NotNull(idField);
        Assert.NotNull(amountField);

        var actualId = (int)idField.GetValue(boxedValue)!;
        var actualAmount = (float)amountField.GetValue(boxedValue)!;

        Assert.False(actualId == expectedId && actualAmount.Equals(expectedAmount));
    }

    public static void AssertSingleFloatFieldStructValue(Type valueType, object boxedValue, string fieldName, float expectedValue)
    {
        var field = valueType.GetField(fieldName);
        Assert.NotNull(field);
        Assert.Equal(expectedValue, (float)field.GetValue(boxedValue)!);
    }

    public static void AssertSingleFloatFieldStructNotValue(Type valueType, object boxedValue, string fieldName, float unexpectedValue)
    {
        var field = valueType.GetField(fieldName);
        Assert.NotNull(field);
        Assert.NotEqual(unexpectedValue, (float)field.GetValue(boxedValue)!);
    }

    public static void AssertMaskValue8(byte[] deltaBytes, byte expectedMask)
    {
        var reader = new NetDataReader(deltaBytes);
        Assert.Equal(expectedMask, reader.GetByte());
    }

    public static void AssertMaskValue16(byte[] deltaBytes, ushort expectedMask)
    {
        var reader = new NetDataReader(deltaBytes);
        Assert.Equal(expectedMask, reader.GetUShort());
    }

    public static void AssertMaskValue32(byte[] deltaBytes, uint expectedMask)
    {
        var reader = new NetDataReader(deltaBytes);
        Assert.Equal(expectedMask, reader.GetUInt());
    }

    public static void AssertMaskValue64(byte[] deltaBytes, ulong expectedMask)
    {
        var reader = new NetDataReader(deltaBytes);
        Assert.Equal(expectedMask, reader.GetULong());
    }

    public static void AssertMaskValue(byte[] deltaBytes, ulong expectedMask, int fieldCount)
    {
        if (fieldCount <= 8)
        {
            AssertMaskValue8(deltaBytes, (byte)expectedMask);
        }
        else if (fieldCount <= 16)
        {
            AssertMaskValue16(deltaBytes, (ushort)expectedMask);
        }
        else if (fieldCount <= 32)
        {
            AssertMaskValue32(deltaBytes, (uint)expectedMask);
        }
        else
        {
            AssertMaskValue64(deltaBytes, expectedMask);
        }
    }
    
    // ---
    
    public static object ParseEnum(Assembly assembly, string enumTypeName, string valueName)
    {
        var enumType = assembly.GetType(enumTypeName);
        Assert.NotNull(enumType);
        return Enum.Parse(enumType, valueName);
    }

    public static object CreateCustomValue(Type customValueType, int id, float amount)
    {
        var value = Activator.CreateInstance(customValueType);
        Assert.NotNull(value);

        var idField = customValueType.GetField("Id");
        var amountField = customValueType.GetField("Amount");

        Assert.NotNull(idField);
        Assert.NotNull(amountField);

        idField.SetValue(value, id);
        amountField.SetValue(value, amount);

        return value;
    }

    public static byte[] InvokeSerialize(object instance)
    {
        var writer = new NetDataWriter();
        Invoke(instance, "Serialize", writer);
        return writer.CopyData();
    }

    public static void InvokeDeserialize(object instance, byte[] bytes)
    {
        var reader = new NetDataReader(bytes);
        Invoke(instance, "Deserialize", reader);
    }

    public static byte[] InvokeWriteDelta(object instance)
    {
        var writer = new NetDataWriter();
        Invoke(instance, "WriteDelta", writer);
        return writer.CopyData();
    }

    public static void InvokeReadDelta(object instance, byte[] bytes)
    {
        var reader = new NetDataReader(bytes);
        Invoke(instance, "ReadDelta", reader);
    }

    public static void InvokeSkipDelta(object instance, byte[] bytes)
    {
        var reader = new NetDataReader(bytes);
        Invoke(instance, "SkipDelta", reader);
        Assert.True(reader.EndOfData);
    }

    public static object? Invoke(object instance, int arity, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == methodName && m.GetParameters().Length == arity);
        Assert.NotNull(method);
        return method.Invoke(instance, args);
    }

    public static object? Invoke(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName);
        Assert.NotNull(method);
        return method.Invoke(instance, args);
    }

    public static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        property.SetValue(instance, value);
    }

    public static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property.GetValue(instance)!;
    }
}