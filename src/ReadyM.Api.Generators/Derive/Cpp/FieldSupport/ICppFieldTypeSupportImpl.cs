using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal interface ICppFieldTypeSupportImpl : IDeriveTypeSupportImplBase
{
    void EmitGetterMethod(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    void EmitSetterMethod(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    void EmitGetterBody(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    void EmitSetterBody(ITypeSymbol symbol, CppEmitFieldSupportContext context);
}