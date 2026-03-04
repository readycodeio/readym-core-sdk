using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

public delegate void SerializeMethod(NetDataWriter writer, object customObject);