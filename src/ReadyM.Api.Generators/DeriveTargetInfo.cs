namespace ReadyM.Api.Generators;

internal class DeriveTargetInfo(
    string name,
    string @namespace,
    DeriveMemberInfo[] members,
    bool isNullable,
    string[] errorMessages,
    string? dirtyMaskType)
{
    public string Name { get; } = name;
    public string Namespace { get; set; } = @namespace;
    public DeriveMemberInfo[] Members { get; } = members;
    public bool IsNullable { get; set; } = isNullable;
    public string[] ErrorMessages { get; } = errorMessages;
    public string? DirtyMaskType { get; } = dirtyMaskType;
}