namespace ReadyM.Api.Generators.TypeTranslation.Model;

internal sealed class Numeric(int value) : ITypeName
{
    public int Value { get; } = value;
}