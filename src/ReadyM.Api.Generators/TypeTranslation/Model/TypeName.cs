namespace ReadyM.Api.Generators.TypeTranslation.Model;

internal sealed class TypeName(string name) : ITypeName
{
    public string Name { get; } = name;
}