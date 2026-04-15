using System.Numerics;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class VectorSerializationExtensions
{
    public static void Serialize(this Vector2 vector, NetDataWriter writer)
    {
        writer.Put(vector.X);
        writer.Put(vector.Y);
    }

    public static void Deserialize(this ref Vector2 vector, NetDataReader reader)
    {
        vector.X = reader.GetFloat();
        vector.Y = reader.GetFloat();
    }
    
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
    
    public static void Serialize(this Vector4 vector, NetDataWriter writer)
    {
        writer.Put(vector.X);
        writer.Put(vector.Y);
        writer.Put(vector.Z);
        writer.Put(vector.W);
    }

    public static void Deserialize(this ref Vector4 vector, NetDataReader reader)
    {
        vector.X = reader.GetFloat();
        vector.Y = reader.GetFloat();
        vector.Z = reader.GetFloat();
        vector.W = reader.GetFloat();
    }
}