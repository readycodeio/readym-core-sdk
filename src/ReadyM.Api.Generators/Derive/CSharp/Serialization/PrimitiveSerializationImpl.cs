using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal sealed class PrimitiveSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
        => context.AppendLine($"writer.Put({context.State.CurrentVar});");
    
    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(context.State.CurrentType.SpecialType);
        context.AppendLine($"{context.State.CurrentVar} = reader.{getMethod}();");
    }
}