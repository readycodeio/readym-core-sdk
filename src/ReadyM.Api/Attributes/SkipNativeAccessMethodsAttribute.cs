using System;

namespace ReadyM.Api.Attributes;

/// <exclude />
[AttributeUsage(AttributeTargets.Field)]
public class SkipNativeAccessMethodsAttribute : Attribute
{
    // empty
}