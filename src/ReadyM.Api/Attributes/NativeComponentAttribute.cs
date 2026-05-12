using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true)]
public class NativeComponentAttribute(
    bool bindDelete = false,
    Type? forType = null,
    bool skipCpp = false) : Attribute
{
    public bool BindDelete { get; } = bindDelete;
    public Type? ForType { get; } = forType;
    public bool SkipCpp { get; } = skipCpp;
}