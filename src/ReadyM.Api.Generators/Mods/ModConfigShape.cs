using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators.Mods;

/// What a class has to look like to be a mod's config, for the generator and the analyzer alike.
internal static class ModConfigShape
{
    public const string DefaultFileName = "config.json";

    public static readonly DiagnosticDescriptor NotCreatable = new(
        "READYM031",
        "Mod config cannot be created",
        "'{0}' is a mod config, so the SDK builds one to fill from the file and hands the same one "
        + "to whatever asks for it. Give it a public parameterless constructor, and make it neither "
        + "abstract nor generic.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoFileName = new(
        "READYM032",
        "Mod config names no file",
        "'{0}' names an empty config file. Leave the name out to read {1}, or give one.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor[] All = [NotCreatable, NoFileName];

    public static Config Read(INamedTypeSymbol config)
    {
        var at = config.Locations.FirstOrDefault() ?? Location.None;
        var problems = new List<Diagnostic>();

        if (!IsCreatable(config))
            problems.Add(Diagnostic.Create(NotCreatable, at, config.Name));

        var fileName = FileNameOf(config);

        if (fileName.Length == 0)
        {
            problems.Add(Diagnostic.Create(NoFileName, at, config.Name, DefaultFileName));
            fileName = DefaultFileName;
        }

        return new Config(fileName, [.. problems]);
    }

    public static bool IsModConfig(ISymbol symbol)
        => symbol.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ModConfigAttribute);

    /// The name the attribute gave, or the default where it named none.
    private static string FileNameOf(INamedTypeSymbol config)
    {
        var attribute = config.GetAttributes().FirstOrDefault(a
            => a.AttributeClass?.ToDisplayString() == ArchetypeNames.ModConfigAttribute);

        if (attribute is null || attribute.ConstructorArguments.Length == 0)
            return DefaultFileName;

        return attribute.ConstructorArguments[0].Value as string ?? DefaultFileName;
    }

    /// What the reader needs of it: new() on a concrete, closed class.
    private static bool IsCreatable(INamedTypeSymbol config)
        => config is { IsAbstract: false, IsStatic: false, TypeParameters.Length: 0 }
           && config.InstanceConstructors.Any(constructor
               => constructor is { Parameters.Length: 0, DeclaredAccessibility: Accessibility.Public });

    internal readonly struct Config(string fileName, Diagnostic[] problems)
    {
        public string FileName { get; } = fileName;

        public Diagnostic[] Problems { get; } = problems;
    }
}
