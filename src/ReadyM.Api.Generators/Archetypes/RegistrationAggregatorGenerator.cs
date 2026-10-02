using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Gathers every registration a compilation emits into one entry point a host can call: what a shape
/// adds to an archetype, what it handles as it is created, and every service the assembly declares.
/// </summary>
/// <remarks>
/// Each registration also runs from a module initializer where the target has them, but that only
/// fires once something in the assembly is touched, and a netstandard2.0 target has none at all. A
/// single well-known type gives a loader something to call outright, before any entity exists.
/// </remarks>
[Generator]
internal class RegistrationAggregatorGenerator : IIncrementalGenerator
{
    private const string Namespace = "ReadyM.SDK.Generated";
    private const string ClassName = "ShapeRegistrations";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var archetypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.ArchetypeAttribute,
            static (node, _) => node is StructDeclarationSyntax,
            Read);

        var mixins = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.MixinAttribute,
            static (node, _) => node is StructDeclarationSyntax,
            Read);

        var services = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.ServiceAttribute,
            static (node, _) => node is ClassDeclarationSyntax,
            ReadService);

        var entries = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.ModEntryAttribute,
            static (node, _) => node is ClassDeclarationSyntax,
            ReadModEntry);

        var configs = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.ModConfigAttribute,
            static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
            ReadModConfig);

        var handlers = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.RpcHandlersForAttribute,
            static (node, _) => node is ClassDeclarationSyntax,
            ReadRpcHandlers);

        // The 0.x spelling of the same attribute, so a mod that has not moved over is collected too.
        var legacyHandlers = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.ServerRpcForAttribute,
            static (node, _) => node is ClassDeclarationSyntax,
            ReadRpcHandlers);

        var all = archetypes.Collect().Combine(mixins.Collect())
            .Combine(services.Collect()).Combine(entries.Collect()).Combine(configs.Collect())
            .Combine(handlers.Collect()).Combine(legacyHandlers.Collect());

        context.RegisterSourceOutput(all, static (spc, found) =>
        {
            var declared = found.Left.Left;

            var names = declared.Left.Left.Left.Left.Concat(declared.Left.Left.Left.Right)
                .Concat(declared.Left.Left.Right).Concat(declared.Left.Right).Concat(declared.Right)
                .Concat(found.Left.Right).Concat(found.Right)
                .SelectMany(entry => entry)
                .Distinct()
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToList();

            if (names.Count == 0)
                return;

            spc.AddSource($"{Namespace}.{ClassName}.g.cs", Emit(names));
        });
    }

    /// <summary>The registration classes one declaration produces, by the names that emit them.</summary>
    private static ImmutableArray<string> Read(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol { ContainingType: null } symbol)
            return [];

        var model = DeclarationModel.For(symbol);
        var found = ImmutableArray.CreateBuilder<string>();
        var ns = ArchetypeNames.NamespaceOf(symbol);
        var prefix = ns.Length == 0 ? "global::" : $"global::{ns}.";

        if (model.Extends.Count > 0)
            found.Add($"{prefix}{symbol.Name}Extends");

        if (model.IndexedBy is not null)
            found.Add($"{prefix}{symbol.Name}Index");

        if (model.RegistersReplication)
            found.Add($"{prefix}{symbol.Name}Replication");

        if (NativeInitEmitter.Applies(model))
            found.Add($"{prefix}{symbol.Name}NativeInit");

        // Handlers the shape declared for itself, which sit inside the shape rather than beside it.
        if (CreateHandlerEmitter.Check(model).Length == 0)
            foreach (var handler in model.CreateHandlers)
                found.Add($"{prefix}{symbol.Name}.{handler.Name}Registration");

        if (DeleteHandlerEmitter.Check(model).Length == 0)
            foreach (var handler in model.DeleteHandlers)
                found.Add($"{prefix}{symbol.Name}.{handler.Name}DeleteRegistration");

        return found.ToImmutable();
    }

    /// <summary>The registration an RPC class produces, if a generator will complete it.</summary>
    /// <remarks>
    /// Both sides carry the same attribute and only one of the two generators claims a class, but
    /// the registration it writes has the same name either way, so this does not have to know which.
    /// </remarks>
    private static ImmutableArray<string> ReadRpcHandlers(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol { ContainingType: null } symbol
            || !ServerRpcModel.Implements(symbol))
            return [];

        var ns = ArchetypeNames.NamespaceOf(symbol);
        var prefix = ns.Length == 0 ? "global::" : $"global::{ns}.";

        return [$"{prefix}{symbol.Name}.Registration"];
    }

    /// The registration a mod's config produces, unless the analyzer refuses the class.
    private static ImmutableArray<string> ReadModConfig(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol { ContainingType: null } symbol)
            return [];

        if (Mods.ModConfigShape.Read(symbol).Problems.Any(problem => problem.Severity == DiagnosticSeverity.Error))
            return [];

        var ns = ArchetypeNames.NamespaceOf(symbol);
        var prefix = ns.Length == 0 ? "global::" : $"global::{ns}.";

        return [$"{prefix}{symbol.Name}.Registration"];
    }

    /// The registration a mod's entry point produces, unless the analyzer refuses the class.
    private static ImmutableArray<string> ReadModEntry(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol { ContainingType: null } symbol)
            return [];

        if (Mods.ModEntryShape.Read(symbol).Problems.Any(problem => problem.Severity == DiagnosticSeverity.Error))
            return [];

        var ns = ArchetypeNames.NamespaceOf(symbol);
        var prefix = ns.Length == 0 ? "global::" : $"global::{ns}.";

        return [$"{prefix}{symbol.Name}.Registration"];
    }

    /// <summary>The registration a service produces, unless the analyzer refuses the class.</summary>
    private static ImmutableArray<string> ReadService(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol { ContainingType: null } symbol)
            return [];

        if (Services.ServiceShape.Read(symbol).Problems.Any(problem => problem.Severity == DiagnosticSeverity.Error))
            return [];

        var ns = ArchetypeNames.NamespaceOf(symbol);
        var prefix = ns.Length == 0 ? "global::" : $"global::{ns}.";

        return [$"{prefix}{symbol.Name}.Registration"];
    }

    private static string Emit(System.Collections.Generic.IReadOnlyList<string> names)
    {
        var writer = new SourceWriter();

        writer.Line("// <auto-generated/>");
        writer.Line("#nullable enable");
        writer.Line();
        writer.Line($"namespace {Namespace};");
        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {ClassName}"))
        using (writer.Braces("public static void RegisterAll()"))
            foreach (var name in names)
                writer.Line($"{name}.Register();");

        return writer.ToString();
    }
}
