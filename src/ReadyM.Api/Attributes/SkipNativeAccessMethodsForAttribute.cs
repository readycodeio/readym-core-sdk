using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
internal class SkipNativeAccessMethodsForAttribute(
    Type forType,
    string forField) : SkipNativeAccessMethodsAttribute
{
    public Type ForType { get; } = forType;
    public string ForField { get; } = forField;
}