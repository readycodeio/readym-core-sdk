using System.Numerics;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class SerializationExtensions
{
    public static void Serialize(this Vector3 vector, NetDataWriter writer)
    {
        writer.Put(vector.X);
        writer.Put(vector.Y);
        writer.Put(vector.Z);
    }

    public static void Deserialize(this ref Vector3 vector, NetDataReader reader)
    {
        vector.X = reader.GetFloat();
        vector.Y = reader.GetFloat();
        vector.Z = reader.GetFloat();
    }
}