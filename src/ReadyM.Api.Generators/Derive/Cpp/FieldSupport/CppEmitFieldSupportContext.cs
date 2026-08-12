namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class CppEmitFieldSupportContext(
    CppEmitState state,
    DeriveMemberModel member,
    DeriveTargetModel model) : CppEmitContextBase(state)
{
    public readonly DeriveMemberModel Member = member;
    public readonly DeriveTargetModel Model = model;

    public string CurrentMaskVar { get; private set; } = "_dirtyMask";

    public void SetCurrentMaskVarName(string maskVarName)
    {
        CurrentMaskVar = maskVarName;
    }
}