using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
public class CppNativeFieldTypeForAttribute(
    Type forType,
    string forField,
    string cppTypeName,
    string defaultValue = "{}",
    string? getterTypeName = null,
    string? setterTypeName = null,
    bool useMove = false,
    Type? fieldType = null,
    bool isReadOnly = false,
    params string[] includes)
    : CppNativeFieldTypeAttribute(
        cppTypeName,
        defaultValue,
        getterTypeName,
        setterTypeName,
        useMove,
        includes)
{
    public Type ForType { get; } = forType;
    public string ForField { get; } = forField;
    public Type? FieldType { get; } = fieldType;
    public bool IsReadOnly { get; } = isReadOnly;
}