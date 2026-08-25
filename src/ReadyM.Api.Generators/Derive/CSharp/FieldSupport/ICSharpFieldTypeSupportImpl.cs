using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal interface ICSharpFieldTypeSupportImpl : IDeriveSupportImplBase<ITypeSymbol>
{
    void EmitDirtyMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitAccessorMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitNotifyChangesMethods(ITypeSymbol sourceType, CSharpEmitFieldSupportContext context);
    void EmitSerializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitDeserializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool resolveConflicts);
    void EmitWriteDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitReadDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool resolveConflicts);

    void EmitFieldEnum(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);

    bool HasDispose(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
    void EmitDisposeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);

    void EmitAssignComponentBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);
}
