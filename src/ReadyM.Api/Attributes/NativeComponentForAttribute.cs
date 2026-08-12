using System;

namespace ReadyM.Api.Attributes;

/// <exclude />
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class NativeComponentForAttribute(
    Type forType,
    bool bindDelete = false,
    bool skipCpp = false) : NativeComponentAttribute(bindDelete, skipCpp)
{
    public Type ForType { get; } = forType;
}