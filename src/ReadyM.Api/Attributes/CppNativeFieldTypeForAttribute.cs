using System;

namespace ReadyM.Api.Attributes;

/// <exclude />
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class CppNativeFieldTypeForAttribute(
    Type forType,
    string forField,
    string cppTypeName,
    string defaultValue = "{}",
    string? getterTypeName = null,
    string? setterTypeName = null,
    bool useMove = false,
    Type? fieldType = null,
    bool readOnly = false,
    params string[] includes)
    : CppNativeFieldTypeAttribute(
        cppTypeName,
        defaultValue,
        getterTypeName,
        setterTypeName,
        useMove,
        readOnly,
        includes)
{
    public Type ForType { get; } = forType;
    public string ForField { get; } = forField;
    public Type? FieldType { get; } = fieldType;
}