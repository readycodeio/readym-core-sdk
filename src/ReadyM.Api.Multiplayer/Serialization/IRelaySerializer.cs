using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

public interface IRelaySerializer
{
    void SerializeObject(NetDataWriter writer, object? data);
    object? DeserializeObject(NetDataReader stream);
    T DeserializeObject<T>(NetDataReader stream);
}