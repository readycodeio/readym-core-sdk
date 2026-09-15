using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

internal enum IncludeKind
{
    Mixin,
    Tag,
    Archetype
}

/// <summary>One partial property an author declared on a mixin or archetype.</summary>
internal sealed class AccessorModel(IPropertySymbol property)
{
    public string Name { get; } = property.Name;

    public string Type { get; } = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public bool HasSetter { get; } = property.SetMethod is not null;

    public string Field { get; } = ArchetypeNames.FieldOf(property.Name);
}

/// <summary>One entry of an <c>[Include]</c> or <c>[IncludeArchetype]</c> on an archetype.</summary>
internal sealed class IncludeModel(INamedTypeSymbol type, IncludeKind kind, bool optional)
{
    public INamedTypeSymbol Type { get; } = type;

    public IncludeKind Kind { get; } = kind;

    public bool Optional { get; } = optional;

    public string TypeName { get; } = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string Parameter { get; } = type.Name.ToLowerFirst();

    public string Component { get; } = ArchetypeNames.QualifiedComponentOf(type);
}

/// <summary>
/// What the generators need to know about one declaration, read from symbols only.
/// </summary>
internal sealed class DeclarationModel
{
    private DeclarationModel(INamedTypeSymbol symbol)
    {
        Symbol = symbol;
        Accessors = ReadAccessors(symbol);
        Includes = ReadIncludes(symbol);
    }

    public INamedTypeSymbol Symbol { get; }

    public IReadOnlyList<AccessorModel> Accessors { get; }

    public IReadOnlyList<IncludeModel> Includes { get; }

    public string Namespace => ArchetypeNames.NamespaceOf(Symbol);

    public string Name => Symbol.Name;

    /// <summary>The struct header a generated part has to agree with.</summary>
    public string Header => Symbol.IsReadOnly ? $"readonly partial struct {Name}" : $"partial struct {Name}";

    /// <summary>Only a declaration with accessors of its own needs a component to keep them in.</summary>
    public bool HasOwnComponent => Accessors.Count > 0;

    public string Component => ArchetypeNames.ComponentOf(Symbol);

    public string QualifiedComponent => ArchetypeNames.QualifiedComponentOf(Symbol);

    public string QualifiedName => Namespace.Length == 0 ? $"global::{Name}" : $"global::{Namespace}.{Name}";

    public static DeclarationModel For(INamedTypeSymbol symbol) => new(symbol);

    /// <summary>
    /// Every component an entity of this archetype carries, its own first, then whatever it includes.
    /// </summary>
    /// <remarks>
    /// Optional includes are left out on purpose: they are what an entity may carry, not what a query
    /// has to match. Including an archetype pulls in that archetype's components too, so this walks.
    /// </remarks>
    public IReadOnlyList<string> RequiredComponents()
    {
        var components = new List<string>();

        if (HasOwnComponent)
            components.Add(QualifiedComponent);

        Collect(this, components, new HashSet<string>());
        return components;
    }

    private static void Collect(DeclarationModel model, List<string> into, HashSet<string> seen)
    {
        foreach (var include in model.Includes)
        {
            if (include.Optional || include.Kind == IncludeKind.Tag)
                continue;

            if (include.Kind == IncludeKind.Archetype)
            {
                var included = For(include.Type);

                if (included.HasOwnComponent && seen.Add(included.QualifiedComponent))
                    into.Add(included.QualifiedComponent);

                Collect(included, into, seen);
                continue;
            }

            if (seen.Add(include.Component))
                into.Add(include.Component);
        }
    }

    /// <summary>
    /// The accessors a mixin contributes, flattened onto whatever includes it, transitively through
    /// included archetypes. A name already taken by the archetype itself wins.
    /// </summary>
    public IReadOnlyList<(IncludeModel Include, AccessorModel Accessor)> FlattenedAccessors()
    {
        var flattened = new List<(IncludeModel, AccessorModel)>();
        var taken = new HashSet<string>(Accessors.Select(accessor => accessor.Name));

        Flatten(this, flattened, taken);
        return flattened;
    }

    private static void Flatten(
        DeclarationModel model,
        List<(IncludeModel, AccessorModel)> into,
        HashSet<string> taken)
    {
        foreach (var include in model.Includes)
        {
            if (include.Kind == IncludeKind.Tag)
                continue;

            if (include.Kind == IncludeKind.Archetype)
            {
                var included = For(include.Type);

                foreach (var accessor in included.Accessors.Where(accessor => taken.Add(accessor.Name)))
                    into.Add((new IncludeModel(included.Symbol, IncludeKind.Archetype, false), accessor));

                Flatten(included, into, taken);
                continue;
            }

            foreach (var accessor in ReadAccessors(include.Type).Where(accessor => taken.Add(accessor.Name)))
                into.Add((include, accessor));
        }
    }

    private static IReadOnlyList<AccessorModel> ReadAccessors(INamedTypeSymbol symbol)
        => symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(IsAccessor)
            .Select(property => new AccessorModel(property))
            .ToList();

    // A declaration in this compilation states its accessors as partial properties. One that arrived
    // as metadata has them compiled already, so the interface members are what has to be skipped.
    private static bool IsAccessor(IPropertySymbol property)
        => property is { IsStatic: false, ExplicitInterfaceImplementations.IsEmpty: true }
           && property.Name is not ("Handle" or "Components" or "IsValid")
           && (property.IsPartialDefinition || property.DeclaredAccessibility == Accessibility.Public);

    private static IReadOnlyList<IncludeModel> ReadIncludes(INamedTypeSymbol symbol)
    {
        var includes = new List<IncludeModel>();

        foreach (var attribute in symbol.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();

            if (attribute.ConstructorArguments.Length == 0 ||
                attribute.ConstructorArguments[0].Value is not INamedTypeSymbol included)
                continue;

            if (name == ArchetypeNames.IncludeArchetypeAttribute)
            {
                includes.Add(new IncludeModel(included, IncludeKind.Archetype, false));
                continue;
            }

            if (name != ArchetypeNames.IncludeAttribute)
                continue;

            var optional = attribute.ConstructorArguments.Length > 1 &&
                           attribute.ConstructorArguments[1].Value is true;

            includes.Add(new IncludeModel(included, KindOf(included), optional));
        }

        return includes;
    }

    private static IncludeKind KindOf(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            switch (attribute.AttributeClass?.ToDisplayString())
            {
                case ArchetypeNames.TagAttribute: return IncludeKind.Tag;
                case ArchetypeNames.ArchetypeAttribute: return IncludeKind.Archetype;
            }
        }

        return IncludeKind.Mixin;
    }
}
