namespace ReadyM.Api.Generators.TypeTranslation.Model;

public sealed class TypeParam(string name) : ITypeName
{
    public string Name { get; } = name;
}