using System.Collections.Generic;

namespace ReadyM.Api.Generators.TypeTranslation.Model;

internal sealed class GenericInstanceName(ITypeName genericDefinition, IReadOnlyList<ITypeName> typeArguments) : ITypeName
{
    public ITypeName GenericDefinition { get; } = genericDefinition;

    public IReadOnlyList<ITypeName> TypeArguments { get; } = typeArguments;
}