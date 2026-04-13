using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal interface ICppFieldTypeSupport
{
    bool CanHandle(ITypeSymbol type);

    string GetCppTypeName(ITypeSymbol type);

    string GetCppDefaultValue(ITypeSymbol type);

    string BuildSetterCondition(DeriveMemberModel model);
}