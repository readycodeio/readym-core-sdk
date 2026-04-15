using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class CppEmitFieldSupportContext(
    CppEmitState state,
    ITypeSymbol maskType,
    int maskIndex) : CppEmitContextBase(state)
{
    public readonly ITypeSymbol MaskType = maskType;
    public readonly int MaskIndex = maskIndex;

    public string CurrentMaskVar { get; private set; } = "_dirtyMask";

    public void SetCurrentMaskVarName(string maskVarName)
    {
        CurrentMaskVar = maskVarName;
    }
}