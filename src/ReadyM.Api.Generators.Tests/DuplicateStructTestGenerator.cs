using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Duplication;

namespace ReadyM.Api.Generators.Tests;

/// <summary>
/// The smallest possible driver for <see cref="TypeDuplicator"/>: finds <c>[DuplicateOf]</c>, calls the engine,
/// adds the file. It exists only so the tests can exercise duplication end to end, from attribute to a loaded
/// assembly. Real callers host <see cref="TypeDuplicator"/> inside their own generator instead.
/// </summary>
[Generator]
internal sealed class DuplicateStructTestGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "ReadyM.Api.Generators.Tests.TestTypes.DuplicateOfAttribute",
            static (node, _) => node is StructDeclarationSyntax,
            static (ctx, _) => (ctx.SemanticModel.Compilation, Target: (INamedTypeSymbol)ctx.TargetSymbol, ctx.Attributes[0]));

        context.RegisterSourceOutput(targets, static (spc, model) =>
        {
            var (compilation, target, attribute) = model;

            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol source)
                return;

            var result = TypeDuplicator.Duplicate(new TypeDuplicationRequest(compilation, source, target)
            {
                ExcludedMemberNames = AttributeUtils.GetAttributeValue(attribute, "Exclude", new string[0])
            });

            if (result.Source is not null)
                spc.AddSource(target.Name + ".Duplicate.g.cs", result.Source);
        });
    }
}
