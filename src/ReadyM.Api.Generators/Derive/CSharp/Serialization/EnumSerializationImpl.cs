using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal sealed class EnumSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => type.TypeKind == TypeKind.Enum;

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
    {
        var baseType = SerializationHelper.GetEnumBaseType(context.State.CurrentType);
        var baseTypeName = SerializationHelper.GetSpecialTypeCSharpName(baseType);
        context.AppendLine($"writer.Put(({baseTypeName}){context.State.CurrentVar});");
    }

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        var baseType = SerializationHelper.GetEnumBaseType(context.State.CurrentType);
        var getMethod = SerializationHelper.GetDeserializationMethod(baseType);
        context.AppendLine($"{context.State.CurrentVar} = ({FullyQualifiedTypeName(context.State.CurrentType)})reader.{getMethod}();");
    }
}