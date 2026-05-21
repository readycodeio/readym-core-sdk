using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal interface ICppFieldTypeSupportImpl : IDeriveSupportImplBase<DeriveMemberModel>
{
    void EmitDirtyMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool emitPublic);
    
    bool HasCreate(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    void EmitTryCreateBody(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    bool HasDispose(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    void EmitDisposeBody(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    
    bool HasAssignComponent(ITypeSymbol sourceType, CppEmitFieldSupportContext context);
    void EmitAssignComponentBody(ITypeSymbol symbol, CppEmitFieldSupportContext context);
    
    void EmitBackingField(ITypeSymbol symbol, CppEmitFieldSupportContext context);
}