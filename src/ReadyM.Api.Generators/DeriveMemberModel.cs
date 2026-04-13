using System;

namespace ReadyM.Api.Generators;

internal sealed class DeriveMemberModel(
    DeriveMemberInfo source,
    string generatedPropertyName,
    int index)
{
    public DeriveMemberInfo SourceMember { get; } = source ?? throw new ArgumentNullException(nameof(source));
    public string GeneratedPropertyName { get; } = generatedPropertyName ?? throw new ArgumentNullException(nameof(generatedPropertyName));
    public int Index { get; } = index;
}