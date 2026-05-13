using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
public class BoolNativeAccessMethodsForAttribute(
    Type forType,
    string forField) : BoolNativeAccessMethodsAttribute
{
    public Type ForType { get; } = forType;
    public string ForField { get; } = forField;
}