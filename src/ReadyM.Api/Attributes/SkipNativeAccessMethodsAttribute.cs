using System;

namespace ReadyM.Api.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class SkipNativeAccessMethodsAttribute : Attribute
{
    // empty
}