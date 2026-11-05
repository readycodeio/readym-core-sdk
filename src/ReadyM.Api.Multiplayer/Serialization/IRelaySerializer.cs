using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

/// <exclude />
/// <summary>
/// Interface for serializing and deserializing objects for relay communication.
/// The type is public because it is used in mod-generated code, but it is not intended for direct use by mod developers.
/// </summary>
public interface IRelaySerializer
{
    void SerializeObject(NetDataWriter writer, object? data);
    object? DeserializeObject(NetDataReader stream);
    T DeserializeObject<T>(NetDataReader stream);
}