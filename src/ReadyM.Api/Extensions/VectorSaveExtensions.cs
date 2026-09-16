using ReadyM.Api.Saves;
using System.Numerics;

namespace ReadyM.Api.Extensions;

public static class VectorSaveExtensions
{
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
