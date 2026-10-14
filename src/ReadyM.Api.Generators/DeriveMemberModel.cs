using System;

namespace ReadyM.Api.Generators;

internal sealed class DeriveMemberModel(
    DeriveMemberInfo source,
    string generatedPropertyName,
    int maskIndex,
    DeriveAccessorMemberSettings accessorSettings,
    DeriveMemberCppSettings cppSettings,
    bool interned,
    DeriveQuantization? quantization = null,
    int quantizationAttributeCount = 0)
{
    public DeriveMemberInfo Source { get; } = source ?? throw new ArgumentNullException(nameof(source));
    public string GeneratedPropertyName { get; } = generatedPropertyName ?? throw new ArgumentNullException(nameof(generatedPropertyName));
    public int MaskIndex { get; } = maskIndex;
    public DeriveAccessorMemberSettings AccessorSettings { get; } = accessorSettings;
    public DeriveMemberCppSettings CppSettings { get; } = cppSettings;
    public bool Interned { get; } = interned;
    public DeriveQuantization? Quantization { get; } = quantization;

    /// How many of [Quantize], [QuantizeAngle] and [HalfPrecision] the field carries; more than one is an error.
    public int QuantizationAttributeCount { get; } = quantizationAttributeCount;
}