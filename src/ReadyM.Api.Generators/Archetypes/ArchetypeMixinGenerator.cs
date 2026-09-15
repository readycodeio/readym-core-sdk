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
            if (generated is not null)
                spc.AddSource(generated.Value.HintName, generated.Value.Source);
        });
    }

    private static (string HintName, string Source)? Emit(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol || symbol.ContainingType is not null)
            return null;

        var model = DeclarationModel.For(symbol);
        var writer = new SourceWriter();

        HandleEmitter.File(writer, model);
        ComponentEmitter.Emit(writer, model.Component, model.Accessors);
        writer.Line();

        using (writer.Braces($"{model.Header} : {ArchetypeNames.Mixin}"))
        {
            HandleEmitter.Handle(writer, model);
            HandleEmitter.Accessors(writer, model.Accessors, model.QualifiedComponent, partial: true);
        }

        return ($"{symbol.Name}.Mixin.g.cs", writer.ToString());
    }
}
