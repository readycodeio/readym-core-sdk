using System;

namespace ReadyM.Api.Generators;

internal sealed class DeriveMemberModel(
    DeriveMemberInfo source,
    string generatedPropertyName,
    int maskIndex,
    DeriveAccessorMemberSettings settings)
{
    public DeriveMemberInfo Source { get; } = source ?? throw new ArgumentNullException(nameof(source));
    public string GeneratedPropertyName { get; } = generatedPropertyName ?? throw new ArgumentNullException(nameof(generatedPropertyName));
    public int MaskIndex { get; } = maskIndex;
    public DeriveAccessorMemberSettings Settings { get; } = settings;
}