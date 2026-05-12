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
    params string[] includes)
    : CppNativeFieldTypeAttribute(
        cppTypeName,
        defaultValue,
        getterTypeName,
        setterTypeName,
        useMove,
        includes)
{
    public readonly Type ForType = forType;
    public readonly string ForField = forField;
}