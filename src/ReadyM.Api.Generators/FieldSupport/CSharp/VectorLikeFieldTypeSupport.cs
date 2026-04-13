using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.CSharp;

internal sealed class VectorLikeFieldTypeSupport : CSharpFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.IsVectorLike(type);

    public override string BuildSetterBody(string maskType, DeriveMemberModel model)
    {
        var dirtySet = SetDirtyMask(maskType, model);
        var type = model.SourceMember.Type;

        return type.Name switch
        {
            "Vector2" => $"if ({FullyQualifiedType(type)}.DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f) {{ {dirtySet} }}",
            "Vector3" => $"if ({FullyQualifiedType(type)}.DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f) {{ {dirtySet} }}",
            "Vector4" => $"if ({FullyQualifiedType(type)}.DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f) {{ {dirtySet} }}",
            _ => throw new InvalidOperationException($"Unsupported vector type: {type.ToDisplayString()}")
        };
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