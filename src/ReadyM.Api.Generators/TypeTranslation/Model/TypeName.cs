namespace ReadyM.Api.Generators.TypeTranslation.Model;

public sealed class TypeName(string name) : ITypeName
{
    public string Name { get; } = name;
}