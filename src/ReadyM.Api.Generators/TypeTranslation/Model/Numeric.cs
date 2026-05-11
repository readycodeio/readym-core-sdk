namespace ReadyM.Api.Generators.TypeTranslation.Model;

public sealed class Numeric(int value) : ITypeName
{
    public int Value { get; } = value;
}