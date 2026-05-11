using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class NativeComponentAttribute(bool bindDelete = false) : Attribute
{
    // empty
}