using System;

namespace ReadyM.SDK.Attributes;

// Declares a method as a command a player or a console can run
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute(string name) : Attribute
{
    // Without the leading slash, matched case insensitively
    public string Name { get; } = name;

    // One line, shown when the arguments are wrong and by a help command
    public string Usage { get; set; } = "";

    // The permission object this needs, or null to let anyone run it
    public string? Permission { get; set; }
}
