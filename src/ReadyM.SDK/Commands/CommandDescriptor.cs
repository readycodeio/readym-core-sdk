using System;
using ReadyM.Api.Command;

namespace ReadyM.SDK.Commands;

public sealed class CommandDescriptor
{
    internal CommandDescriptor(string name, Delegate handler, string usage, string? permission)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A command with no name can never be typed.", nameof(name));

        Name = name;
        Usage = string.IsNullOrEmpty(usage) ? "/" + name : usage;
        Permission = permission;
        Binding = CommandBinding.Of(handler);
    }

    public string Name { get; }

    public string Usage { get; }

    public string? Permission { get; }

    internal CommandBinding Binding { get; }

    internal ConsoleCommand Console => Binding.Console;
}
