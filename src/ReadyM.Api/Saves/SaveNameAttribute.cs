using System;

namespace ReadyM.Api.Saves;

/// <summary>
/// Overrides the stable save name of a savable component.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class SaveNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
