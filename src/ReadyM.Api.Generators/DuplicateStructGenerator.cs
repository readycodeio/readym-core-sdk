using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ReadyM.Api.Generators.Duplication;

namespace ReadyM.Api.Generators;

/// <summary>
/// Generates the other half of a partial struct marked <c>[DuplicateOf(typeof(Other))]</c>, filling it with a copy
/// of the named struct's members.
///
/// This type is only the plumbing: finding the attribute, reading its options, mapping engine issues to
/// diagnostics, and adding the file. The copying itself lives in <see cref="TypeDuplicator"/>.
/// </summary>
[Generator]
internal sealed class DuplicateStructGenerator : IIncrementalGenerator
{
    private const string Category = "Duplication";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            DuplicateStructAttributeSource.HintName,
            SourceText.From(DuplicateStructAttributeSource.Text, Encoding.UTF8)));

        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            DuplicateStructAttributeSource.MetadataName,
            predicate: static (node, _) => node is StructDeclarationSyntax,
            transform: static (ctx, _) => Read(ctx));

        context.RegisterSourceOutput(
            targets.Where(static model => model is not null),
            static (spc, model) => Execute(spc, model!));
    }

    private static DuplicateStructModel? Read(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol target)
            return null;

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null)
            return null;

        var location = attribute.ApplicationSyntaxReference is { } reference
            ? Location.Create(reference.SyntaxTree, reference.Span)
            : target.Locations.FirstOrDefault() ?? Location.None;

        if (attribute.ConstructorArguments.Length == 0 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol source)
        {
            return new DuplicateStructModel(context.SemanticModel.Compilation, target, source: null, location);
        }

        return new DuplicateStructModel(context.SemanticModel.Compilation, target, source, location)
        {
            Exclude = AttributeUtils.GetAttributeValue(attribute, "Exclude", new string[0]),
            CopyAttributes = AttributeUtils.GetAttributeValue(attribute, "CopyAttributes", true),
            CopyDocumentation = AttributeUtils.GetAttributeValue(attribute, "CopyDocumentation", true),
            CopyInterfaces = AttributeUtils.GetAttributeValue(attribute, "CopyInterfaces", true)
        };
    }

    private static void Execute(SourceProductionContext context, DuplicateStructModel model)
    {
        if (model.Source is null)
        {
            Report(context, "DUP001", "Unresolved duplication source", model.Location,
                $"The type passed to [DuplicateOf] on '{model.Target.Name}' could not be resolved.");
            return;
        }

        var request = new TypeDuplicationRequest(model.Compilation, model.Source, model.Target)
        {
            ExcludedMemberNames = model.Exclude,
            CopyAttributes = model.CopyAttributes,
            CopyDocumentation = model.CopyDocumentation,
            CopyInterfaces = model.CopyInterfaces
        };

        var result = TypeDuplicator.Duplicate(request);

        foreach (var issue in result.Issues)
            Report(context, IdFor(issue.Code), TitleFor(issue.Code), model.Location, issue.Message);

        if (result.Source is null)
            return;

        var hintName = model.Target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '{')
            .Replace('>', '}');

        context.AddSource(hintName + ".Duplicate.g.cs", SourceText.From(result.Source, Encoding.UTF8));
    }

    private static void Report(
        SourceProductionContext context,
        string id,
        string title,
        Location location,
        string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(id, title, message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true),
            location));
    }

    private static string IdFor(TypeDuplicationIssueCode code) => code switch
    {
        TypeDuplicationIssueCode.SourceNotStruct => "DUP002",
        TypeDuplicationIssueCode.SourceNotInCompilation => "DUP003",
        TypeDuplicationIssueCode.TargetNotPartial => "DUP004",
        TypeDuplicationIssueCode.GenericArityMismatch => "DUP005",
        TypeDuplicationIssueCode.SourceIsTarget => "DUP006",
        _ => "DUP000"
    };

    private static string TitleFor(TypeDuplicationIssueCode code) => code switch
    {
        TypeDuplicationIssueCode.SourceNotStruct => "Duplication source is not a struct",
        TypeDuplicationIssueCode.SourceNotInCompilation => "Duplication source is not in this compilation",
        TypeDuplicationIssueCode.TargetNotPartial => "Duplication target is not partial",
        TypeDuplicationIssueCode.GenericArityMismatch => "Duplication arity mismatch",
        TypeDuplicationIssueCode.SourceIsTarget => "Duplication source is the target",
        _ => "Duplication failed"
    };

    /// <summary>What the generator carries from the syntax pass to the output pass.</summary>
    private sealed class DuplicateStructModel(
        Compilation compilation,
        INamedTypeSymbol target,
        INamedTypeSymbol? source,
        Location location)
    {
        public Compilation Compilation { get; } = compilation;

        public INamedTypeSymbol Target { get; } = target;

        public INamedTypeSymbol? Source { get; } = source;

        public Location Location { get; } = location;

        public string[] Exclude { get; set; } = new string[0];

        public bool CopyAttributes { get; set; } = true;

        public bool CopyDocumentation { get; set; } = true;

        public bool CopyInterfaces { get; set; } = true;
    }
}
