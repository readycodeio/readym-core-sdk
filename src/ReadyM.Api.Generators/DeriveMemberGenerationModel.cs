using System;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal sealed class DeriveMemberGenerationModel(
    DeriveMemberInfo member,
    int index,
    string generatedPropertyName,
    bool isSupported,
    bool usePutGet,
    bool isEnum,
    SpecialType enumBaseType,
    bool isEquatable,
    bool isDeltaEquatable,
    bool isCustomSerializable,
    bool isVector)
{
    public DeriveMemberInfo Member { get; } = member ?? throw new ArgumentNullException(nameof(member));
    public int Index { get; } = index;
    public string GeneratedPropertyName { get; } = generatedPropertyName ?? throw new ArgumentNullException(nameof(generatedPropertyName));
    public bool IsSupported { get; } = isSupported;
    public bool UsePutGet { get; } = usePutGet;
    public bool IsEnum { get; } = isEnum;
    public SpecialType EnumBaseType { get; } = enumBaseType;
    public bool IsEquatable { get; } = isEquatable;
    public bool IsDeltaEquatable { get; } = isDeltaEquatable;
    public bool IsCustomSerializable { get; } = isCustomSerializable;
    public bool IsVector { get; } = isVector;
}