using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Marks a <c>[Tag]</c> struct as a tag the store understands.
/// </summary>
[Generator]
internal class TagGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tags = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.TagAttribute,
            static (node, _) => node is StructDeclarationSyntax,
            Emit);

        context.RegisterSourceOutput(tags, static (spc, generated) =>
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
        writer.Line($"{model.Header} : {ArchetypeNames.Tag};");

        return ($"{symbol.Name}.Tag.g.cs", writer.ToString());
    }
}
