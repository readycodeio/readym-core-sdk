using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class NativeComponentAttribute : Attribute
{
    // empty
}