using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class SkipNativeAccessMethodsAttribute : Attribute
{
    // empty
}