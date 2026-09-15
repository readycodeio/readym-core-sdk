using System;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal sealed class NetDataSerializationCodec : ICSharpSerializationCodec
{
    public void WritePrimitive(CSharpEmitSerializeContext context)
        => context.AppendLine($"writer.Put({context.State.CurrentVar});");

    public void ReadPrimitive(CSharpEmitDeserializeContext context)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(context.State.CurrentType.SpecialType);
        context.AppendLine($"{context.State.CurrentVar} = reader.{getMethod}();");
    }

    public void WriteEnum(CSharpEmitSerializeContext context)
    {
        var baseType = SerializationHelper.GetEnumBaseType(context.State.CurrentType);
        var baseTypeName = SerializationHelper.GetSpecialTypeCSharpName(baseType);
        context.AppendLine($"writer.Put(({baseTypeName}){context.State.CurrentVar});");
    }

    public void ReadEnum(CSharpEmitDeserializeContext context)
    {
        var baseType = SerializationHelper.GetEnumBaseType(context.State.CurrentType);
        var getMethod = SerializationHelper.GetDeserializationMethod(baseType);
        context.AppendLine($"{context.State.CurrentVar} = ({FullyQualifiedTypeName(context.State.CurrentType)})reader.{getMethod}();");
    }

    public void WriteNativeString(CSharpEmitSerializeContext context)
    {
        context.State.ModuleState.AddUsing("Yooni.Native.Serialization");
        context.AppendLine($"{context.State.CurrentVar}.Serialize(writer);");
    }

    public void ReadNativeString(CSharpEmitDeserializeContext context)
    {
        context.State.ModuleState.AddUsing("Yooni.Native.Serialization");
        context.AppendLine($"{context.State.CurrentVar}.Deserialize(reader);");
    }

    public void WriteSelf(CSharpEmitSerializeContext context)
        => context.AppendLine($"{context.State.CurrentVar}.Serialize(writer);");

    public void ReadSelf(CSharpEmitDeserializeContext context)
        => context.AppendLine($"{context.State.CurrentVar}.Deserialize(reader);");

    public void SerializeCollection(CSharpEmitSerializeContext context, string sourceVar, string iterVar, Action emitElement)
    {
        var countVar = context.MethodState.NewVarName("count");
        context.AppendLine($"var {countVar} = {sourceVar}.IsCreated ? {sourceVar}.Count : 0;");
        context.AppendLine($"writer.Put({countVar});");
        context.AppendLine($"if ({countVar} > 0)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"foreach (var {iterVar} in {sourceVar})");
            using (context.WithCodeBlock())
            {
                emitElement();
            }
        }
    }

    public void DeserializeCollection(CSharpEmitDeserializeContext context, string targetVar, Action emitElement)
    {
        var indexVar = context.MethodState.NewVarName("index");
        var countVar = context.MethodState.NewVarName("count");
        context.AppendLine($"var {countVar} = reader.GetInt();");
        context.AppendLine($"{targetVar}.Clear();");
        context.AppendLine($"for (var {indexVar} = 0; {indexVar} < {countVar}; {indexVar}++)");
        using (context.WithCodeBlock())
        {
            emitElement();
        }
    }
}
