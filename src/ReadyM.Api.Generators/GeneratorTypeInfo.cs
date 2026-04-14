namespace ReadyM.Api.Generators;

internal class GeneratorTypeInfo(
    string name,
    string @namespace,
    GeneratorField[] members,
    bool isNullable,
    string[] errorMessages,
    string? dirtyMaskType)
{
    public string Name { get; } = name;
    public string Namespace { get; set; } = @namespace;
    public GeneratorField[] Members { get; } = members;
    public bool IsNullable { get; set; } = isNullable;
    public string[] ErrorMessage { get; } = errorMessages;
    public string? DirtyMaskType { get; } = dirtyMaskType;
}