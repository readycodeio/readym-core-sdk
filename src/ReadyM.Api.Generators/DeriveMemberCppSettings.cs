using System.Collections.Generic;

namespace ReadyM.Api.Generators;

internal readonly struct DeriveMemberCppSettings(
    string? cppTypeName,
    string? defaultValue,
    string? getterTypeName,
    string? setterTypeName,
    bool useMove,
    params IReadOnlyList<string> includes)
{
    public string? CppTypeName { get; } = cppTypeName;
    public string? DefaultValue { get; } = defaultValue;
    public string? GetterTypeName { get; } = getterTypeName;
    public string? SetterTypeName { get; } = setterTypeName;
    public bool UseMove { get; } = useMove;
    public IReadOnlyList<string> Includes { get; } = includes;
}