using System.Collections.Immutable;
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
        // The side is a build property, so it reaches the transform by being combined in rather
        // than read from the syntax context, which carries no such thing.
        var client = context.AnalyzerConfigOptionsProvider.Select(static (options, _) => SdkRuntime.IsClient(options));

        var archetypes = context.SyntaxProvider.ForAttributeWithMetadataName(
                ArchetypeNames.ArchetypeAttribute,
                static (node, _) => node is StructDeclarationSyntax,
                static (syntax, _) => (Symbol: syntax.TargetSymbol as INamedTypeSymbol, syntax.SemanticModel.Compilation))
            .Combine(client)
            .Select(static (pair, ct) => Emit(pair.Left.Symbol, pair.Left.Compilation, pair.Right, ct));

        // A reference to the server SDK without its chunk types means they moved or were renamed.
        // Saying so beats turning the fast path off and letting a profiler find it months later.
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
        {
            if (ChunkNames.ChunkAssemblyReferenced(compilation) && ChunkNames.Resolve(compilation) is null)
                spc.ReportDiagnostic(Diagnostic.Create(ChunkTypesMissing, Location.None));
        });

        context.RegisterSourceOutput(archetypes, static (spc, generated) =>
        {
            if (generated is null)
                return;

            foreach (var diagnostic in generated.Value.Diagnostics)
                spc.ReportDiagnostic(diagnostic);

            spc.AddSource(generated.Value.HintName, generated.Value.Source);

            // A replicated component is a file of its own: the emitter writes the usings and the
            // namespace itself, and two file-scoped namespaces cannot share a file.
            if (generated.Value.Component is { } component)
                spc.AddSource(component.HintName, component.Source);
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

    private static (string HintName, string Source, ImmutableArray<Diagnostic> Diagnostics,
        (string HintName, string Source)? Component)? Emit(INamedTypeSymbol? target, Compilation compilation, bool client, CancellationToken ct)
    {
        if (target is not { ContainingType: null } symbol)
            return null;

        var model = DeclarationModel.For(symbol);
        var problems = ExplicitComponentRules.Check(model, compilation)
            .AddRange(ReplicationRules.Check(model))
            .AddRange(CreateHandlerEmitter.Check(model))
            .AddRange(DeleteHandlerEmitter.Check(model))
            .AddRange(ReplicationRules.CheckPropagation(model));
        // Accessors reachable from a chunk go on whenever the server SDK is there, because another
        // declaration may include this one. The view itself needs this shape to be walkable.
        var chunks = ChunkNames.Resolve(compilation);
        var view = model.SupportsChunks ? chunks : null;

        var writer = new SourceWriter();

        HandleEmitter.File(writer, model);

        (string HintName, string Source)? replicated = null;

        if (model.EmitsComponent && model.IsReplicated)
        {
            var described = ReplicatedComponentModel.For(model, compilation);

            if (described is not null)
                replicated = (
                    ArchetypeNames.HintOf(symbol, "Component"),
                    ReplicatedComponentEmitter.Emit(
                        described,
                        PropagationContracts.Of(model.Propagation)));

            ComponentEmitter.EmitFields(writer, model.Component, model.Accessors);
            writer.Line();
        }
        else if (model.EmitsComponent)
        {
            ComponentEmitter.Emit(writer, model.Component, model.Accessors);
            writer.Line();
        }

        if (model.NeedsMarker)
        {
            ComponentEmitter.EmitMarker(writer, model.Marker);
            writer.Line();
        }

        AccessorEmitter.Emit(writer, model, HandleEmitter.ComponentSet(model), chunks, chunkWrites: !client);
        writer.Line();
        CollectionAccessEmitter.Emit(writer, model);
        writer.Line();

        using (writer.Braces($"{model.Header} : {ArchetypeNames.Archetype}{IndexEmitter.Contract(model)}"))
        {
            HandleEmitter.Handle(writer, model);
            EmitIdentity(writer);
            HandleEmitter.Accessors(writer, model.Accessors, model.QualifiedAccessors, partial: true, model.IsReplicated);
            EmitIncluded(writer, model, client);
            EmitForwards(writer, model);
            EmitConversions(writer, model);
            EmitScope(writer, model);
            ValuesEmitter.Emit(writer, model);
            CreateHandlerEmitter.Emit(writer, model, compilation);
            DeleteHandlerEmitter.Emit(writer, model, compilation);
        }

        if (view is not null)
        {
            writer.Line();
            ChunkViewEmitter.Emit(writer, model, view, chunkWrites: !client);
            writer.Line();
            ChunkViewEmitter.EmitQueryBinding(writer, model, view);
        }

        ExtendsEmitter.Emit(writer, model, compilation);
        IndexEmitter.Emit(writer, model, compilation);
        ReplicationEmitter.Emit(writer, model, compilation);
        NativeInitEmitter.Emit(writer, model, compilation);

        return (ArchetypeNames.HintOf(symbol, "Archetype"), writer.ToString(), problems, replicated);
    }

    private static void EmitIdentity(SourceWriter writer)
    {
        writer.Line();
        writer.Line("public bool IsValid => _handle.IsAlive();");
        writer.Line();
        writer.Line($"public bool Is<T>() where T : struct, {ArchetypeNames.Queryable} => _handle.Is<T>();");
        writer.Line();
        writer.Line($"public T As<T>() where T : struct, {ArchetypeNames.Queryable} => _handle.As<T>();");
        writer.Line();
        writer.Line($"public bool TryAs<T>(out T archetype) where T : struct, {ArchetypeNames.Queryable}");
        writer.Line("    => _handle.TryAs(out archetype);");
    }

    private static void EmitScope(SourceWriter writer, DeclarationModel model)
    {
        if (!model.IsScope)
            return;

        writer.Line();
        writer.Line($"public static implicit operator {ArchetypeNames.Scope}({model.QualifiedName} value)");
        writer.Line($"    => {ArchetypeNames.Scope}.Of(value);");
    }

    /// <summary>Collections of everything included, flattened onto the archetype.</summary>
    private static void EmitForwards(SourceWriter writer, DeclarationModel model)
    {
        foreach (var forward in model.Forwards)
            AccessorEmitter.Forwarded(writer, forward, model.QualifiedAccessors);

        foreach (var (include, forward) in model.FlattenedForwards())
            AccessorEmitter.Forwarded(writer, forward, include.Accessors, include.Named(forward));
    }

    /// <summary>Accessors of everything included, flattened onto the archetype.</summary>
    private static void EmitIncluded(SourceWriter writer, DeclarationModel model, bool client)
    {
        foreach (var (include, accessor) in model.FlattenedAccessors())
        {
            writer.Line();
            HandleEmitter.Accessor(
                writer, accessor, include.Accessors, partial: false, include.IsReplicated, client,
                include.Named(accessor.Name));
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
