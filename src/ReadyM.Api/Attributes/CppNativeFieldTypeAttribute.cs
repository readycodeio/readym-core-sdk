using System;
using System.Collections.Generic;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class CppNativeFieldTypeAttribute(
    string cppTypeName,
    string defaultValue = "{}",
    string? getterTypeName = null,
    string? setterTypeName = null,
    bool useMove = false,
    bool readOnly = false,
    params string[] includes) : Attribute
{
    public readonly string CppTypeName = cppTypeName;
    public readonly string? GetterName = getterTypeName;
    public readonly string? SetterType = setterTypeName;
    public readonly string DefaultValue = defaultValue;
    public readonly bool UseMove = useMove;
    public readonly bool ReadOnly = readOnly;
    public readonly IReadOnlyList<string> Includes = includes;
}