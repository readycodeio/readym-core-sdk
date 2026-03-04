using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

public class RelaySerializer
{
    private byte _nextTypeCode = 255;

    public byte HashtableTypeCode;

    private readonly Dictionary<Type, (byte Code, SerializeMethod Serialize, DeserializeMethod Deserialize)> _registeredTypes = new();
    private readonly Dictionary<byte, (Type Type, SerializeMethod Serialize, DeserializeMethod Deserialize)> _code2Type = new();

    public RelaySerializer(IEnumerable<IRelaySerializerRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            registration.Register(this);
        }
    }

    public byte RegisterType(
        Type customType,
        SerializeMethod serializeMethod,
        DeserializeMethod deserializeMethod)
    {
        // check if already registered
        if (_registeredTypes.ContainsKey(customType))
        {
            throw new ArgumentException($"Type {customType} is already registered");
        }

        var code = _nextTypeCode--;

        _registeredTypes[customType] = (code, serializeMethod, deserializeMethod);
        _code2Type[code] = (customType, serializeMethod, deserializeMethod);

        return code;
    }

    [Obsolete]
    public static Dictionary<object, object?> UpdateAndGetDiff(
        Dictionary<object, object> state,
        IEnumerable<(object, object?)> changes)
    {
        var diff = new Dictionary<object, object?>();

        foreach (var (key, newVal) in changes)
        {
            if (state.TryGetValue(key, out var oldVal))
            {
                if (newVal == null)
                {
                    state.Remove(key);
                    diff[key] = null;
                }
                else if (oldVal != newVal)
                {
                    diff[key] = newVal;
                    state[key] = newVal;
                }
            }
            else if (newVal != null)
            {
                state[key] = newVal;
                diff[key] = newVal;
            }
        }

        return diff;
    }

    [Obsolete]
    public static Dictionary<object, object?> UpdateAndGetDiff(
        Dictionary<object, object> state,
        Dictionary<object, object?> changes)
    {
        return UpdateAndGetDiff(state, changes.Select(kv => (kv.Key, kv.Value)));
    }

    /// <summary>
    /// Writes the object to the stream.
    /// Format:
    /// - PlayerId size (2 bytes)
    /// - byte type code (1 byte)
    /// - data
    /// </summary>
    public void SerializeObject(NetDataWriter writer, object? data)
    {
        if (data == null)
        {
            writer.Put((ushort)0);
            return;
        }

        var type = data.GetType();
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        if (!_registeredTypes.TryGetValue(type, out var typeInfo))
        {
            throw new ArgumentException($"Type {data.GetType()} is not registered");
        }

        var sizeOffset = writer.Length;
        writer.Put((ushort)0);
        writer.Put(typeInfo.Code);
        typeInfo.Serialize(writer, data);
        var afterDataOffset = writer.Length;
        var size = (ushort)(afterDataOffset - sizeOffset - 2);
        writer.SetPosition(sizeOffset);
        writer.Put(size);
        writer.SetPosition(afterDataOffset);
    }

    /// <summary>
    /// Deserializes the object from the stream.
    /// The data must be in the same format as serialized by SerializeObject.
    /// </summary>
    public object? DeserializeObject(NetDataReader stream)
    {
        var size = stream.GetUShort();
        if (size == 0)
        {
            return null;
        }

        var typeCode = stream.GetByte();

        if (!_code2Type.TryGetValue(typeCode, out var typeInfo))
        {
            throw new ArgumentException($"Type code {typeCode} is not registered");
        }

        return typeInfo.Deserialize(stream);
    }

    public T DeserializeObject<T>(NetDataReader stream)
    {
        try
        {
            return (T)DeserializeObject(stream)!;
        }
        catch
        {
            var size = BitConverter.ToUInt16(stream.RawData, stream.UserDataOffset);
            var typeCode = stream.RawData[stream.UserDataOffset + 2];
            throw new SerializationException($"Failed to deserialize object of type {typeof(T)}, received {size} bytes of type {typeCode}");
        }
    }
}