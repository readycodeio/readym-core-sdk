using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal class NativeComponentForAttribute(
    Type forType,
    bool bindDelete = false,
    bool skipCpp = false) : NativeComponentAttribute(bindDelete, skipCpp)
{
    public Type ForType { get; } = forType;
}