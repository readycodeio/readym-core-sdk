namespace ReadyM.Api.Generators;

internal readonly struct DeriveAccessorMemberSettings(bool skipAccessors, bool boolAccessors)
{
    public bool SkipAccessors { get; } = skipAccessors;
    public bool BoolAccessors { get; } = boolAccessors;
}