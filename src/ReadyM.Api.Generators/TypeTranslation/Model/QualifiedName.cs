namespace ReadyM.Api.Generators.TypeTranslation.Model;

public sealed class QualifiedName(ITypeName prefix, ITypeName innerType) : ITypeName
{
    public ITypeName Prefix { get; } = prefix;

    public ITypeName InnerType { get; } = innerType;
}