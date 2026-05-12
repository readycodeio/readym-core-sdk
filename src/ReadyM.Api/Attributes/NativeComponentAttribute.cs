using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class NativeComponentAttribute(
    bool bindDelete = false,
    bool skipCpp = false) : Attribute
{
    public bool BindDelete { get; } = bindDelete;
    public bool SkipCpp { get; } = skipCpp;
}