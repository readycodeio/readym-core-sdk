using System;

namespace ReadyM.Api.Generators;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class WrapperIncludeAttribute : Attribute
{
    public string Regex { get; }
    public WrapperIncludeAttribute(string regex) => Regex = regex;
}