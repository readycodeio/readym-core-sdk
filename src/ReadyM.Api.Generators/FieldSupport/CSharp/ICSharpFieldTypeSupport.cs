using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.CSharp;

internal interface ICSharpFieldTypeSupport
{
    bool CanHandle(ITypeSymbol type);

    string BuildSetterBody(string maskType, DeriveMemberModel model);

    void EmitSerialize(StringBuilder sb, DeriveMemberModel model);

    void EmitDeserialize(StringBuilder sb, string maskType, DeriveMemberModel model);

    void EmitWriteDelta(StringBuilder sb, string maskType, DeriveMemberModel model);

    void EmitReadDelta(StringBuilder sb, string maskType, DeriveMemberModel model);

    void EmitSkipDelta(StringBuilder sb, string maskType, DeriveMemberModel model);
}