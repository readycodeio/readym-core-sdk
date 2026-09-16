using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Completes an <c>[Archetype]</c> struct: the component its own accessors live in, everything its
/// includes contribute, and the conversions between it and the archetypes it includes.
/// </summary>
[Generator]
internal class ArchetypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var archetypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.ArchetypeAttribute,
            static (node, _) => node is StructDeclarationSyntax,
            Emit);

        // A reference to the server SDK without its chunk types means they moved or were renamed.
        // Saying so beats turning the fast path off and letting a profiler find it months later.
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
        {
            if (ChunkNames.ChunkAssemblyReferenced(compilation) && ChunkNames.Resolve(compilation) is null)
                spc.ReportDiagnostic(Diagnostic.Create(ChunkTypesMissing, Location.None));
        });

        context.RegisterSourceOutput(archetypes, static (spc, generated) =>
        {
            if (generated is not null)
                spc.AddSource(generated.Value.HintName, generated.Value.Source);
        });
    }

    private static readonly DiagnosticDescriptor ChunkTypesMissing = new(
        "READYM001",
        "Chunk fast path disabled",
        $"{ChunkNames.ChunkAssembly} is referenced but its chunk types were not found, so every query "
        + "walks identities instead of chunks. They were probably moved or renamed.",
        "ReadyM",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static (string HintName, string Source)? Emit(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol { ContainingType: null } symbol)
            return null;

        var model = DeclarationModel.For(symbol);
        // Accessors reachable from a chunk go on whenever the server SDK is there, because another
        // declaration may include this one. The view itself needs this shape to be walkable.
        var chunks = ChunkNames.Resolve(context.SemanticModel.Compilation);
        var view = model.SupportsChunks ? chunks : null;

        var writer = new SourceWriter();

        HandleEmitter.File(writer, model);

        if (model.HasOwnComponent)
        {
            ComponentEmitter.Emit(writer, model.Component, model.Accessors);
            writer.Line();
        }

        ComponentEmitter.EmitMarker(writer, model.Marker);
        writer.Line();

        AccessorEmitter.Emit(writer, model, HandleEmitter.ComponentSet(model), chunks);
        writer.Line();

        using (writer.Braces($"{model.Header} : {ArchetypeNames.Archetype}"))
        {
            HandleEmitter.Handle(writer, model);
            EmitIdentity(writer);
            HandleEmitter.Accessors(writer, model.Accessors, model.QualifiedAccessors, partial: true);
            EmitIncluded(writer, model);
            EmitConversions(writer, model);
        }

        if (view is not null)
        {
            writer.Line();
            ChunkViewEmitter.Emit(writer, model, view);
            writer.Line();
            ChunkViewEmitter.EmitQueryBinding(writer, model, view);
        }

        return (ArchetypeNames.HintOf(symbol, "Archetype"), writer.ToString());
    }

    private static void EmitIdentity(SourceWriter writer)
    {
        writer.Line();
        writer.Line("public bool IsValid => _handle.IsAlive();");
        writer.Line();
        writer.Line($"public bool Is<T>() where T : struct, {ArchetypeNames.Queryable} => _handle.Is<T>();");
        writer.Line();
        writer.Line($"public bool TryAs<T>(out T archetype) where T : struct, {ArchetypeNames.Queryable}");
        writer.Line("    => _handle.TryAs(out archetype);");
    }

    /// <summary>Accessors of everything included, flattened onto the archetype.</summary>
    private static void EmitIncluded(SourceWriter writer, DeclarationModel model)
    {
        foreach (var (include, accessor) in model.FlattenedAccessors())
        {
            writer.Line();
            HandleEmitter.Accessor(writer, accessor, include.Accessors, partial: false);
        }
    }

    /// <summary>
    /// Including an archetype gives a conversion up, always allowed, and a checked one back down.
    /// </summary>
    private static void EmitConversions(SourceWriter writer, DeclarationModel model)
    {
        foreach (var include in model.Includes.Where(i => i.Kind == IncludeKind.Archetype))
        {
            writer.Line();
            writer.Line($"public static implicit operator {include.TypeName}({model.QualifiedName} value)");
            writer.Line("    => new(value._handle);");
            writer.Line();

            using (writer.Braces($"public static explicit operator {model.QualifiedName}({include.TypeName} value)"))
            {
                writer.Line($"var handle = {ArchetypeNames.EntityHandle}.Of(value);");
                writer.Line();
                writer.Line($"if (!handle.Is<{model.QualifiedName}>())");
                writer.Line($"    throw new global::System.InvalidCastException($\"{{handle}} is not a {model.Name}.\");");
                writer.Line();
                writer.Line($"return new {model.QualifiedName}(handle);");
            }
        }
    }
}
