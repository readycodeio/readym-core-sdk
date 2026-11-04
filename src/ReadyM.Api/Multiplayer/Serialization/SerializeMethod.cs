using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

internal delegate void SerializeMethod(NetDataWriter writer, object customObject);