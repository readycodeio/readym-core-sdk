using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Completes a <c>[ArchetypeMixin]</c> struct: the component its values live in, and the accessors
/// reading them.
/// </summary>
[Generator]
internal class ArchetypeMixinGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // The side is a build property, so it reaches the transform by being combined in rather
        // than read from the syntax context, which carries no such thing.
        var client = context.AnalyzerConfigOptionsProvider.Select(static (options, _) => SdkRuntime.IsClient(options));

        var mixins = context.SyntaxProvider.ForAttributeWithMetadataName(
                ArchetypeNames.MixinAttribute,
                static (node, _) => node is StructDeclarationSyntax,
                static (syntax, _) => (Symbol: syntax.TargetSymbol as INamedTypeSymbol, syntax.SemanticModel.Compilation))
            .Combine(client)
            .Select(static (pair, ct) => Emit(pair.Left.Symbol, pair.Left.Compilation, pair.Right, ct));

        context.RegisterSourceOutput(mixins, static (spc, generated) =>
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
        AccessorEmitter.Emit(writer, model, HandleEmitter.ComponentSet(model), chunks, chunkWrites: !client);
        writer.Line();
        CollectionAccessEmitter.Emit(writer, model);
        writer.Line();

        using (writer.Braces($"{model.Header} : {ArchetypeNames.Mixin}{IndexEmitter.Contract(model)}"))
        {
            HandleEmitter.Handle(writer, model);
            HandleEmitter.Accessors(writer, model.Accessors, model.QualifiedAccessors, partial: true, model.IsReplicated);

            foreach (var forward in model.Forwards)
                AccessorEmitter.Forwarded(writer, forward, model.QualifiedAccessors);

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
        ExtendedMemberEmitter.Emit(writer, model, chunks, client);
        IndexEmitter.Emit(writer, model, compilation);
        ReplicationEmitter.Emit(writer, model, compilation);
        NativeInitEmitter.Emit(writer, model, compilation);

        return (ArchetypeNames.HintOf(symbol, "Mixin"), writer.ToString(), problems, replicated);
    }
}
