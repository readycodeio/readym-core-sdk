using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace ReadyM.Api.Generators;

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1009")]
internal sealed class DummySymbol(
    string name,
    ITypeSymbol type,
    INamedTypeSymbol? containingType = null,
    Accessibility accessibility = Accessibility.Private,
    bool isStatic = false)
    : IFieldSymbol
{
    public SymbolKind Kind => SymbolKind.Field;
    public string Name => name;
    public string MetadataName => name;
    public ITypeSymbol Type => type;

    public INamedTypeSymbol? ContainingType { get; } = containingType;
    public ISymbol? ContainingSymbol => ContainingType;
    public IAssemblySymbol? ContainingAssembly => ContainingType?.ContainingAssembly;
    public IModuleSymbol? ContainingModule => ContainingType?.ContainingModule;
    public INamespaceSymbol? ContainingNamespace => ContainingType?.ContainingNamespace;

    public Accessibility DeclaredAccessibility { get; } = accessibility;
    public IFieldSymbol OriginalDefinition => this;
    public IFieldSymbol? CorrespondingTupleField => null;
    public bool IsExplicitlyNamedTupleElement => false;

    ISymbol ISymbol.OriginalDefinition => OriginalDefinition;

    public bool HasUnsupportedMetadata => false;
    public bool IsStatic { get; } = isStatic;
    public bool IsConst => false;
    public bool IsReadOnly => false;
    public bool IsVolatile => false;
    public bool IsRequired => false;
    public bool HasConstantValue => false;
    public object? ConstantValue => null;
    public RefKind RefKind => RefKind.None;
    public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;
    public ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;
    public ISymbol? AssociatedSymbol => null;
    public bool IsFixedSizeBuffer => false;
    public int FixedSize => 0;
    public NullableAnnotation NullableAnnotation => NullableAnnotation.None;

    public bool CanBeReferencedByName => true;
    public bool IsImplicitlyDeclared => false;
    public bool IsAbstract => false;
    public bool IsDefinition => true;
    public bool IsExtern => false;
    public bool IsOverride => false;
    public bool IsSealed => false;
    public bool IsVirtual => false;
    public bool IsAsync => false;
    public bool IsObsolete() => false;

    public string Language => LanguageNames.CSharp;
    public int MetadataToken => 0;

    public ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
    public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

    public ImmutableArray<AttributeData> GetAttributes() => ImmutableArray<AttributeData>.Empty;

    public void Accept(SymbolVisitor visitor) => visitor.VisitField(this);

    public TResult? Accept<TResult>(SymbolVisitor<TResult> visitor)
        => visitor.VisitField(this);

    public TResult Accept<TArgument, TResult>(
        SymbolVisitor<TArgument, TResult> visitor,
        TArgument argument)
        => visitor.VisitField(this, argument);

    public string? GetDocumentationCommentId()
        => null;

    public string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false,
        CancellationToken cancellationToken = new CancellationToken())
        => null;

    public string ToDisplayString(SymbolDisplayFormat? format = null) => name;

    public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat? format = null)
        => ImmutableArray.Create(new SymbolDisplayPart(SymbolDisplayPartKind.FieldName, this, name));

    public string ToMinimalDisplayString(
        SemanticModel semanticModel,
        int position,
        SymbolDisplayFormat? format = null)
        => name;

    public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
        SemanticModel semanticModel,
        int position,
        SymbolDisplayFormat? format = null)
        => ToDisplayParts(format);

    public bool Equals(ISymbol? other, SymbolEqualityComparer equalityComparer)
        => ReferenceEquals(this, other);
    
    public bool Equals(ISymbol? other)
        => other is DummySymbol dummy && Name == dummy.Name;

    public override string ToString() => name;
}