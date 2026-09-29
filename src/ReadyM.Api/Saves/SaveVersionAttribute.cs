using System;

namespace ReadyM.Api.Saves;

/// <summary>
/// Overrides the schema version stamped on a savable component's records.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class SaveVersionAttribute(ushort version) : Attribute
{
    public ushort Version { get; } = version;
}
