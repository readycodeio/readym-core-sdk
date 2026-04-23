using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal interface ICppFieldTypeSupportImpl : IDeriveTypeSupportImplBase
{
    void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context);
}