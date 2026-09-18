using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

/// <summary>
/// The save codec: writes into the format-neutral ISaveWriter/ISaveReader.
/// </summary>
internal sealed class SaveSerializationCodec : ICSharpSerializationCodec
{
    public void WritePrimitive(CSharpEmitSerializeContext context)
        => context.AppendLine($"writer.Write({context.State.CurrentVar});");

    public void ReadPrimitive(CSharpEmitDeserializeContext context)
    {
        var readMethod = ReadMethod(context.State.CurrentType.SpecialType);
        context.AppendLine($"{context.State.CurrentVar} = reader.{readMethod}();");
    }

    public void WriteEnum(CSharpEmitSerializeContext context)
    {
        var baseType = SerializationHelper.GetEnumBaseType(context.State.CurrentType);
        var baseTypeName = SerializationHelper.GetSpecialTypeCSharpName(baseType);
        context.AppendLine($"writer.Write(({baseTypeName}){context.State.CurrentVar});");
    }

    public void ReadEnum(CSharpEmitDeserializeContext context)
    {
        var baseType = SerializationHelper.GetEnumBaseType(context.State.CurrentType);
        var readMethod = ReadMethod(baseType);
        context.AppendLine($"{context.State.CurrentVar} = ({FullyQualifiedTypeName(context.State.CurrentType)})reader.{readMethod}();");
    }

    // A fixed-size native string keeps its wide flag alongside the text.
    public void WriteNativeString(CSharpEmitSerializeContext context)
    {
        var v = context.State.CurrentVar;
        context.AppendLine("writer.BeginObject();");
        context.AppendLine("writer.Name(\"wide\");");
        context.AppendLine($"writer.Write({v}.IsWide);");
        context.AppendLine("writer.Name(\"value\");");
        context.AppendLine($"writer.Write({v}.ToString());");
        context.AppendLine("writer.EndObject();");
    }

    public void ReadNativeString(CSharpEmitDeserializeContext context)
    {
        var v = context.State.CurrentVar;
        var typeName = FullyQualifiedTypeName(context.State.CurrentType);
        var wideVar = context.MethodState.NewVarName("wide");
        var strVar = context.MethodState.NewVarName("str");
        context.AppendLine("reader.BeginObject();");
        context.AppendLine("reader.Name(\"wide\");");
        context.AppendLine($"var {wideVar} = reader.ReadBool();");
        context.AppendLine("reader.Name(\"value\");");
        context.AppendLine($"var {strVar} = reader.ReadString();");
        context.AppendLine($"{v} = new {typeName}({strVar}, {wideVar});");
        context.AppendLine("reader.EndObject();");
    }

    public void WriteSelf(CSharpEmitSerializeContext context)
        => context.AppendLine($"{context.State.CurrentVar}.WriteSave(writer, context);");

    public void ReadSelf(CSharpEmitDeserializeContext context)
        => context.AppendLine($"{context.State.CurrentVar}.ReadSave(reader, context);");

    public void SerializeCollection(CSharpEmitSerializeContext context, string sourceVar, string iterVar, Action emitElement)
    {
        var countVar = context.MethodState.NewVarName("count");
        context.AppendLine($"var {countVar} = {sourceVar}.IsCreated ? {sourceVar}.Count : 0;");
        context.AppendLine($"writer.BeginArray({countVar});");
        context.AppendLine($"if ({countVar} > 0)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"foreach (var {iterVar} in {sourceVar})");
            using (context.WithCodeBlock())
            {
                emitElement();
            }
        }
        context.AppendLine("writer.EndArray();");
    }

    public void DeserializeCollection(CSharpEmitDeserializeContext context, string targetVar, Action emitElement)
    {
        context.AppendLine("reader.BeginArray();");
        context.AppendLine($"{targetVar}.Clear();");
        context.AppendLine("while (reader.HasMoreElements())");
        using (context.WithCodeBlock())
        {
            emitElement();
        }
        context.AppendLine("reader.EndArray();");
    }

    public void SerializeDictionary(
        CSharpEmitSerializeContext context, string sourceVar, string iterVar,
        string keyVar, string valueVar, Action emitKey, Action emitValue)
    {
        var countVar = context.MethodState.NewVarName("count");
        context.AppendLine($"var {countVar} = {sourceVar}.IsCreated ? {sourceVar}.Count : 0;");
        context.AppendLine($"writer.BeginArray({countVar});");
        context.AppendLine($"if ({countVar} > 0)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"foreach (var {iterVar} in {sourceVar})");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"var {keyVar} = {iterVar}.Key;");
                context.AppendLine($"var {valueVar} = {iterVar}.Value;");
                context.AppendLine("writer.BeginObject();");
                context.AppendLine("writer.Name(\"k\");");
                emitKey();
                context.AppendLine("writer.Name(\"v\");");
                emitValue();
                context.AppendLine("writer.EndObject();");
            }
        }
        context.AppendLine("writer.EndArray();");
    }

    public void DeserializeDictionary(
        CSharpEmitDeserializeContext context, string targetVar,
        string keyVar, string keyTypeFqn, Action emitKeyRead,
        string valueVar, string valueTypeFqn, Action emitValueRead)
    {
        context.AppendLine("reader.BeginArray();");
        context.AppendLine($"{targetVar}.Clear();");
        context.AppendLine("while (reader.HasMoreElements())");
        using (context.WithCodeBlock())
        {
            context.AppendLine("reader.BeginObject();");
            context.AppendLine($"var {keyVar} = default({keyTypeFqn});");
            context.AppendLine($"var {valueVar} = default({valueTypeFqn});");
            context.AppendLine("reader.Name(\"k\");");
            emitKeyRead();
            context.AppendLine("reader.Name(\"v\");");
            emitValueRead();
            context.AppendLine("reader.EndObject();");
            context.AppendLine($"{targetVar}.Add({keyVar}, {valueVar});");
        }
        context.AppendLine("reader.EndArray();");
    }

    private static string ReadMethod(SpecialType specialType)
        => specialType switch
        {
            SpecialType.System_Boolean => "ReadBool",
            SpecialType.System_Byte => "ReadByte",
            SpecialType.System_SByte => "ReadSByte",
            SpecialType.System_Int16 => "ReadShort",
            SpecialType.System_UInt16 => "ReadUShort",
            SpecialType.System_Int32 => "ReadInt",
            SpecialType.System_UInt32 => "ReadUInt",
            SpecialType.System_Int64 => "ReadLong",
            SpecialType.System_UInt64 => "ReadULong",
            SpecialType.System_Single => "ReadFloat",
            SpecialType.System_Double => "ReadDouble",
            SpecialType.System_String => "ReadString",
            _ => throw new ArgumentException($"Unsupported save primitive: {specialType}"),
        };
}
