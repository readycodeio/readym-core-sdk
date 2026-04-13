using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.CSharp;

internal sealed class CustomSerializableFieldTypeSupport : CSharpFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.HasSerializeMethod(type) && SerializationHelper.HasDeserializeMethod(type);

    public override string BuildSetterBody(string maskType, DeriveMemberModel model)
    {
        var dirtySet = SetDirtyMask(maskType, model);
        return $"if ({model.SourceMember.Name} != value) {{ {dirtySet} }}";
    }

    public override void EmitSerialize(StringBuilder sb, DeriveMemberModel model)
        => sb.AppendLine($"{model.SourceMember.Name}.Serialize(writer);");

    public override void EmitDeserialize(StringBuilder sb, string maskType, DeriveMemberModel model)
        => sb.AppendLine($"{{ {model.SourceMember.Name}.Deserialize(reader); _dirtyMask |= ({maskType})1 << {model.Index}; }}");

    public override void EmitWriteDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
        => sb.AppendLine($"if ((mask & (({maskType})1 << {model.Index})) != 0) {model.SourceMember.Name}.Serialize(writer);");

    public override void EmitReadDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
        => sb.AppendLine($"if ((mask & (({maskType})1 << {model.Index})) != 0) {{ {model.SourceMember.Name}.Deserialize(reader); _dirtyMask |= ({maskType})1 << {model.Index}; }}");

    public override void EmitSkipDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
        => sb.AppendLine($"if ((mask & (({maskType})1 << {model.Index})) != 0) {{ var dummy = default({FullyQualifiedType(model.SourceMember.Type)}); dummy.Deserialize(reader); }}");
}