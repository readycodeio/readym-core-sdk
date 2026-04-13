using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.CSharp;

internal abstract class CSharpFieldTypeSupportBase : ICSharpFieldTypeSupport
{
    public abstract bool CanHandle(ITypeSymbol type);

    public abstract string BuildSetterBody(string maskType, DeriveMemberModel model);

    public abstract void EmitSerialize(StringBuilder sb, DeriveMemberModel model);

    public abstract void EmitDeserialize(StringBuilder sb, string maskType, DeriveMemberModel model);

    public abstract void EmitWriteDelta(StringBuilder sb, string maskType, DeriveMemberModel model);

    public abstract void EmitReadDelta(StringBuilder sb, string maskType, DeriveMemberModel model);

    public abstract void EmitSkipDelta(StringBuilder sb, string maskType, DeriveMemberModel model);

    protected static string SetDirtyMask(string maskType, DeriveMemberModel model)
        => $"{model.SourceMember.Name} = value; _dirtyMask |= ({maskType})1 << {model.Index};";

    protected static string FullyQualifiedType(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}