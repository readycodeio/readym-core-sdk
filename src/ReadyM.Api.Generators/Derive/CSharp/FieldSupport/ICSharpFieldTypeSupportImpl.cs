using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal interface ICSharpFieldTypeSupportImpl : IDeriveTypeSupportImplBase
{
    void EmitAccessorMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitSerializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitDeserializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitWriteDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitReadDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitSkipDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitFieldEnum(ITypeSymbol sourceType, CSharpEmitFieldSupportContext context);
}