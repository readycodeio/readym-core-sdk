using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.CSharp;

internal sealed class EnumFieldTypeSupport : CSharpFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => type.TypeKind == TypeKind.Enum;

    public override string BuildSetterBody(string maskType, DeriveMemberModel model)
    {
        var dirtySet = DirtySet(maskType, model);
        return $"if ({model.SourceMember.Name} != value) {{ {dirtySet} }}";
    }

    public override void EmitSerialize(StringBuilder sb, DeriveMemberModel model)
    {
        var baseType = SerializationHelper.GetSpecialTypeCSharpName(
            SerializationHelper.GetEnumBaseType(model.SourceMember.Type));
        sb.AppendLine($"            writer.Put(({baseType}){model.SourceMember.Name});");
    }

    public override void EmitDeserialize(StringBuilder sb, string maskType, DeriveMemberModel model)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(
            SerializationHelper.GetEnumBaseType(model.SourceMember.Type));
        sb.AppendLine($"            {model.GeneratedPropertyName} = ({FullyQualifiedType(model.SourceMember.Type)})reader.{getMethod}();");
    }

    public override void EmitWriteDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
    {
        var baseType = SerializationHelper.GetSpecialTypeCSharpName(
            SerializationHelper.GetEnumBaseType(model.SourceMember.Type));
        sb.AppendLine($"            if ((mask & (({maskType})1 << {model.Index})) != 0) writer.Put(({baseType}){model.SourceMember.Name});");
    }

    public override void EmitReadDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(
            SerializationHelper.GetEnumBaseType(model.SourceMember.Type));
        sb.AppendLine($"            if ((mask & (({maskType})1 << {model.Index})) != 0) {model.GeneratedPropertyName} = ({FullyQualifiedType(model.SourceMember.Type)})reader.{getMethod}();");
    }

    public override void EmitSkipDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(
            SerializationHelper.GetEnumBaseType(model.SourceMember.Type));
        sb.AppendLine($"            if ((mask & (({maskType})1 << {model.Index})) != 0) reader.{getMethod}();");
    }
}