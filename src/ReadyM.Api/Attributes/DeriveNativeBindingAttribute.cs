using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public class DeriveNativeBindingAttribute : Attribute
{
    // empty
}