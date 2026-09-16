using System.Numerics;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Api.Multiplayer.Extensions;

/// <exclude />
/// <summary>
/// Used in mod-generated code for serializing fields of custom components.
/// </summary>
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

    public static void WriteSave(this in Vector2 vector, ISaveWriter writer)
    {
        writer.BeginObject();
        writer.Name("x");
        writer.Write(vector.X);
        writer.Name("y");
        writer.Write(vector.Y);
        writer.EndObject();
    }

    public static void ReadSave(this ref Vector2 vector, ISaveReader reader)
    {
        reader.BeginObject();
        reader.Name("x");
        vector.X = reader.ReadFloat();
        reader.Name("y");
        vector.Y = reader.ReadFloat();
        reader.EndObject();
    }

    public static void WriteSave(this in Vector3 vector, ISaveWriter writer)
    {
        writer.BeginObject();
        writer.Name("x");
        writer.Write(vector.X);
        writer.Name("y");
        writer.Write(vector.Y);
        writer.Name("z");
        writer.Write(vector.Z);
        writer.EndObject();
    }

    public static void ReadSave(this ref Vector3 vector, ISaveReader reader)
    {
        reader.BeginObject();
        reader.Name("x");
        vector.X = reader.ReadFloat();
        reader.Name("y");
        vector.Y = reader.ReadFloat();
        reader.Name("z");
        vector.Z = reader.ReadFloat();
        reader.EndObject();
    }

    public static void WriteSave(this in Vector4 vector, ISaveWriter writer)
    {
        writer.BeginObject();
        writer.Name("x");
        writer.Write(vector.X);
        writer.Name("y");
        writer.Write(vector.Y);
        writer.Name("z");
        writer.Write(vector.Z);
        writer.Name("w");
        writer.Write(vector.W);
        writer.EndObject();
    }

    public static void ReadSave(this ref Vector4 vector, ISaveReader reader)
    {
        reader.BeginObject();
        reader.Name("x");
        vector.X = reader.ReadFloat();
        reader.Name("y");
        vector.Y = reader.ReadFloat();
        reader.Name("z");
        vector.Z = reader.ReadFloat();
        reader.Name("w");
        vector.W = reader.ReadFloat();
        reader.EndObject();
    }
}
