using System.Linq;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators.Commands;

/// One [Command] method, checked for the shape the dispatcher needs before anything is written.
internal readonly struct CommandShape(string name, string method, string usage, string? permission)
{
    public string Name { get; } = name;
    public string Method { get; } = method;
    public string Usage { get; } = usage;
    public string? Permission { get; } = permission;

    public static CommandShape? Read(IMethodSymbol method, SourceProductionContext spc)
    {
        var attribute = method.GetAttributes().FirstOrDefault(
            a => a.AttributeClass?.ToDisplayString() == ArchetypeNames.CommandAttribute);

        if (attribute is null) return null;

        var name = attribute.ConstructorArguments.FirstOrDefault().Value as string;

        if (string.IsNullOrWhiteSpace(name))
        {
            spc.ReportDiagnostic(Diagnostic.Create(CommandDiagnostics.NeedsAName, method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        if (method.Parameters.Length == 0
            || method.Parameters[0].Type.ToDisplayString() != "ReadyM.SDK.Commands.CommandContext")
        {
            spc.ReportDiagnostic(Diagnostic.Create(CommandDiagnostics.NeedsAContext, method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        if (method.IsStatic)
        {
            spc.ReportDiagnostic(Diagnostic.Create(CommandDiagnostics.CannotBeStatic, method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        var usage = Named(attribute, "Usage") ?? "/" + name;
        var permission = Named(attribute, "Permission");

        return new CommandShape(name!, method.Name, usage, permission);
    }

    private static string? Named(AttributeData attribute, string key)
        => attribute.NamedArguments.FirstOrDefault(pair => pair.Key == key).Value.Value as string;
}
