using System;

namespace ReadyM.Api.Generators;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
internal sealed class WrapperIncludeAttribute(string regex) : Attribute
{
    public string Regex { get; } = regex;
}