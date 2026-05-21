namespace ReadyM.Api.Generators.TypeTranslation.Model;

internal sealed class TypeParam(string name) : ITypeName
{
    public string Name { get; } = name;
}