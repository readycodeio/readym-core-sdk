using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.CSharp;

internal sealed class PrimitiveFieldTypeSupport : CSharpFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    public override string BuildSetterBody(string maskType, DeriveMemberModel model)
    {
        var fieldName = model.SourceMember.Name;
        var type = model.SourceMember.Type;

        if (type == null)
            throw new InvalidOperationException("Member type unexpectedly null.");

        var dirtySet = DirtySet(maskType, model);

        if (type.SpecialType == SpecialType.System_Single)
            return $"if (Math.Abs({fieldName} - value) > {DeriveComponentUtils.FloatComparisonEpsilon}f) {{ {dirtySet} }}";

        if (type.SpecialType == SpecialType.System_Double)
            return $"if (Math.Abs({fieldName} - value) > {DeriveComponentUtils.DoubleComparisonEpsilon}) {{ {dirtySet} }}";

        return $"if ({fieldName} != value) {{ {dirtySet} }}";
    }

    public override void EmitSerialize(StringBuilder sb, DeriveMemberModel model)
        => sb.AppendLine($"            writer.Put({model.SourceMember.Name});");

    public override void EmitDeserialize(StringBuilder sb, string maskType, DeriveMemberModel model)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(model.SourceMember.Type.SpecialType);
        sb.AppendLine($"            {model.GeneratedPropertyName} = reader.{getMethod}();");
    }

    public override void EmitWriteDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
        => sb.AppendLine($"            if ((mask & (({maskType})1 << {model.Index})) != 0) writer.Put({model.SourceMember.Name});");

    public override void EmitReadDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(model.SourceMember.Type.SpecialType);
        sb.AppendLine($"            if ((mask & (({maskType})1 << {model.Index})) != 0) {model.GeneratedPropertyName} = reader.{getMethod}();");
    }

    public override void EmitSkipDelta(StringBuilder sb, string maskType, DeriveMemberModel model)
    {
        var getMethod = SerializationHelper.GetDeserializationMethod(model.SourceMember.Type.SpecialType);
        sb.AppendLine($"            if ((mask & (({maskType})1 << {model.Index})) != 0) reader.{getMethod}();");
    }
}