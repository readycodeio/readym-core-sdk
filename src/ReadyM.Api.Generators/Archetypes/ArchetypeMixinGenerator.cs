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
        var mixins = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.MixinAttribute,
            static (node, _) => node is StructDeclarationSyntax,
            Emit);

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
        (string HintName, string Source)? Component)? Emit(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol || symbol.ContainingType is not null)
            return null;

        var model = DeclarationModel.For(symbol);
        var problems = ExplicitComponentRules.Check(model, context.SemanticModel.Compilation)
            .AddRange(ReplicationRules.Check(model));
        // Accessors reachable from a chunk go on whenever the server SDK is there, because another
        // declaration may include this one. The view itself needs this shape to be walkable.
        var chunks = ChunkNames.Resolve(context.SemanticModel.Compilation);
        var view = model.SupportsChunks ? chunks : null;

        var writer = new SourceWriter();

        HandleEmitter.File(writer, model);

        (string HintName, string Source)? replicated = null;

        if (model.EmitsComponent && model.IsReplicated)
        {
            var described = ReplicatedComponentModel.For(model, context.SemanticModel.Compilation);

            if (described is not null)
                replicated = (
                    ArchetypeNames.HintOf(symbol, "Component"),
                    ReplicatedComponentEmitter.Emit(described));

            ComponentEmitter.EmitFields(writer, model.Component, model.Accessors);
            writer.Line();
        }
        else if (model.EmitsComponent)
        {
            ComponentEmitter.Emit(writer, model.Component, model.Accessors);
            writer.Line();
        }
        AccessorEmitter.Emit(writer, model, HandleEmitter.ComponentSet(model), chunks);
        writer.Line();

        using (writer.Braces($"{model.Header} : {ArchetypeNames.Mixin}{IndexEmitter.Contract(model)}"))
        {
            HandleEmitter.Handle(writer, model);
            HandleEmitter.Accessors(writer, model.Accessors, model.QualifiedAccessors, partial: true, model.IsReplicated);

            foreach (var forward in model.Forwards)
                AccessorEmitter.Forwarded(writer, forward, model.QualifiedAccessors);

            ValuesEmitter.Emit(writer, model);
        }

        if (view is not null)
        {
            writer.Line();
            ChunkViewEmitter.Emit(writer, model, view);
            writer.Line();
            ChunkViewEmitter.EmitQueryBinding(writer, model, view);
        }

        ExtendsEmitter.Emit(writer, model, context.SemanticModel.Compilation);
        ExtendedMemberEmitter.Emit(writer, model, chunks);
        IndexEmitter.Emit(writer, model, context.SemanticModel.Compilation);
        ReplicationEmitter.Emit(writer, model, context.SemanticModel.Compilation);
        NativeInitEmitter.Emit(writer, model, context.SemanticModel.Compilation);

        return (ArchetypeNames.HintOf(symbol, "Mixin"), writer.ToString(), problems, replicated);
    }
}
