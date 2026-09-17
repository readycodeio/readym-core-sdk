using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

internal enum IncludeKind
{
    Mixin,
    Archetype
}

/// <summary>One partial property an author declared on a mixin or archetype.</summary>
internal sealed class AccessorModel(string name, string type, bool hasSetter, string? field = null)
{
    public AccessorModel(IPropertySymbol property, INamedTypeSymbol? component)
        : this(
            property.Name,
            property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            property.SetMethod is not null && (component is null || Assignable(Target(component, property))),
            component is null ? null : Target(component, property)?.Name)
    {
        Declared = property;
        Component = component;
    }

    public string Name { get; } = name;

    public string Type { get; } = type;

    public bool HasSetter { get; } = hasSetter;

    /// <summary>The member holding the value: a generated field, or one of an explicit component.</summary>
    public string Field { get; } = field ?? ArchetypeNames.FieldOf(name);

    /// <summary>Set only while the declaration is in this compilation, which is when it can be checked.</summary>
    public IPropertySymbol? Declared { get; }

    public INamedTypeSymbol? Component { get; }

    /// <summary>The field or property an explicit component holds this value in, matched by name.</summary>
    public static ISymbol? Target(INamedTypeSymbol component, IPropertySymbol property)
        => component.GetMembers()
            .FirstOrDefault(member => member is IFieldSymbol { IsStatic: false, IsConst: false } or IPropertySymbol { IsStatic: false }
                && string.Equals(member.Name, MemberNameOf(property), System.StringComparison.OrdinalIgnoreCase));

    public static bool Assignable(ISymbol? target) => target switch
    {
        IFieldSymbol field => !field.IsReadOnly,
        IPropertySymbol property => property.SetMethod is not null,
        _ => false
    };

    private static string MemberNameOf(IPropertySymbol property)
    {
        foreach (var attribute in property.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ExplicitMemberAttribute
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string member)
                return member;

        return property.Name;
    }
}

/// <summary>One entry of an <c>[Include]</c> or <c>[IncludeArchetype]</c> on an archetype.</summary>
internal sealed class IncludeModel(INamedTypeSymbol type, IncludeKind kind)
{
    public INamedTypeSymbol Type { get; } = type;

    public IncludeKind Kind { get; } = kind;

    public string TypeName { get; } = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string Parameter { get; } = type.Name.ToLowerFirst();

    /// <summary>The class a consumer goes through, wherever the declaration was compiled.</summary>
    public string Accessors { get; } = ArchetypeNames.QualifiedAccessorsOf(type);
}

/// <summary>One entry in a shape's component set: a declaration's component, or its marker.</summary>
internal sealed class ComponentOwner(IncludeModel? include, bool marker)
{
    /// <summary>Null when it belongs to the declaration itself rather than to something it includes.</summary>
    public IncludeModel? Include { get; } = include;

    public bool IsMarker { get; } = marker;
}

/// <summary>
/// What the generators need to know about one declaration, read from symbols only.
/// </summary>
internal sealed class DeclarationModel
{
    private DeclarationModel(INamedTypeSymbol symbol)
    {
        Symbol = symbol;
        ExplicitComponent = ExplicitComponentOf(symbol);
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

    /// <summary>The component named by [ExplicitComponent], or null when one is generated.</summary>
    public INamedTypeSymbol? ExplicitComponent { get; }

    /// <summary>Only a declaration with accessors of its own needs a component to keep them in.</summary>
    public bool HasOwnComponent => Accessors.Count > 0 || ExplicitComponent is not null;

    /// <summary>False when the storage already exists, in which case nothing is emitted for it.</summary>
    public bool EmitsComponent => HasOwnComponent && ExplicitComponent is null;

    public string Component => ArchetypeNames.ComponentOf(Symbol);

    public string Marker => ArchetypeNames.MarkerOf(Symbol);

    public string QualifiedMarker => ArchetypeNames.QualifiedMarkerOf(Symbol);

    /// <summary>Only an archetype gets a marker: a mixin is found by the component it carries.</summary>
    public bool IsArchetype => Symbol.GetAttributes().Any(attribute
        => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ArchetypeAttribute);

    public string QualifiedComponent => ExplicitComponent is null
        ? ArchetypeNames.QualifiedComponentOf(Symbol)
        : ExplicitComponent.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>
    /// An archetype carrying storage it does not own gets no marker: the entities it describes are
    /// created by whoever owns that storage, and would never carry one.
    /// </summary>
    public bool NeedsMarker => IsArchetype && !CarriesExplicit();

    private bool CarriesExplicit()
        => ExplicitComponent is not null || Includes.Any(include => For(include.Type).CarriesExplicit());

    public string QualifiedAccessors => ArchetypeNames.QualifiedAccessorsOf(Symbol);

    public string QualifiedName => Namespace.Length == 0 ? $"global::{Name}" : $"global::{Namespace}.{Name}";

    /// <summary>
    /// Every declaration contributing a component, in the order the component set lists them: this
    /// one's own component first, then each include expanded the same way.
    /// </summary>
    /// <remarks>
    /// Null stands for this declaration's own component; anything else is the include that owns it.
    /// Duplicates are dropped keeping the first, because <c>ComponentSet.Combine</c> does the same
    /// and the two orders have to agree exactly or a chunk view reads the wrong bytes.
    /// </remarks>
    public IReadOnlyList<ComponentOwner> ComponentOwners()
    {
        var owners = new List<ComponentOwner>();

        Collect(this, null, owners, new HashSet<string>());
        return owners;
    }

    private static void Collect(
        DeclarationModel model,
        IncludeModel? owner,
        List<ComponentOwner> owners,
        HashSet<string> seen)
    {
        if (model.HasOwnComponent && seen.Add(model.QualifiedName))
            owners.Add(new ComponentOwner(owner, marker: false));

        if (model.NeedsMarker && seen.Add(model.QualifiedName + " marker"))
            owners.Add(new ComponentOwner(owner, marker: true));

        foreach (var include in model.Includes)
        {
            if (include.Kind == IncludeKind.Archetype)
            {
                Collect(For(include.Type), include, owners, seen);
                continue;
            }

            var mixin = For(include.Type);

            if (mixin.HasOwnComponent && seen.Add(mixin.QualifiedName))
                owners.Add(new ComponentOwner(include, marker: false));
        }
    }

    /// <summary>
    /// Whether every component this shape carries can be reached from a chunk.
    /// </summary>
    /// <remarks>
    /// The order is always known, since there are no optional includes and an included archetype is
    /// flattened in the order its own set lists them. What can still stop it is a contributor
    /// declared in an assembly compiled without the server SDK, which has no chunk accessors.
    /// </remarks>
    public bool SupportsChunks
        => ComponentOwners().Count > 0
           && Contributors().All(contributor => contributor.HasChunkAccessors);

    /// <summary>
    /// Every declaration whose component this shape carries, itself included when it has one.
    /// </summary>
    /// <remarks>
    /// Walks includes transitively, so a contributor from an assembly without the server SDK is
    /// caught however deep it sits. Including an archetype that has no components of its own is not
    /// a problem: it simply contributes nothing.
    /// </remarks>
    private IEnumerable<DeclarationModel> Contributors()
    {
        if (HasOwnComponent)
            yield return this;

        // A marker has no accessors, so nothing about it can be unreachable.
        foreach (var owner in ComponentOwners())
            if (owner is { IsMarker: false, Include: not null })
                yield return For(owner.Include.Type);
    }

    /// <summary>
    /// Whether this declaration's values can be reached from a chunk at all.
    /// </summary>
    /// <remarks>
    /// One declared here will have the accessors emitted alongside it. One that arrived as metadata
    /// only has them if the assembly that declared it was itself compiled against the server SDK, so
    /// the accessor class is what has to be asked.
    /// </remarks>
    private bool HasChunkAccessors
        => !Symbol.DeclaringSyntaxReferences.IsEmpty || AccessorClassTakesChunks(Symbol);

    private static bool AccessorClassTakesChunks(INamedTypeSymbol symbol)
    {
        var accessors = symbol.ContainingNamespace?
            .GetTypeMembers(AccessorEmitter.ClassNameOf(symbol.Name))
            .FirstOrDefault();

        return accessors is not null && accessors.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(method => method.IsStatic
                           && method.Parameters.Length > 1
                           && method.Parameters[0].Type.Name == "ComponentChunk");
    }

    public static DeclarationModel For(INamedTypeSymbol symbol) => new(symbol);

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
            if (include.Kind == IncludeKind.Archetype)
            {
                var included = For(include.Type);

                foreach (var accessor in included.Accessors.Where(accessor => taken.Add(accessor.Name)))
                    into.Add((include, accessor));

                Flatten(included, into, taken);
                continue;
            }

            foreach (var accessor in ReadAccessors(include.Type).Where(accessor => taken.Add(accessor.Name)))
                into.Add((include, accessor));
        }
    }

    /// <summary>
    /// The accessors declared directly on a type, never the ones it inherited by including something.
    /// </summary>
    /// <remarks>
    /// A declaration in this compilation states them as partial properties. One that arrived as
    /// metadata has them compiled, and its flattened accessors look exactly the same, so the accessor
    /// class is what tells them apart: it carries a Get method per accessor the type owns.
    /// </remarks>
    private static IReadOnlyList<AccessorModel> ReadAccessors(INamedTypeSymbol symbol)
    {
        if (symbol.DeclaringSyntaxReferences.IsEmpty)
            return FromAccessorClass(symbol);

        var component = ExplicitComponentOf(symbol);

        return symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => property.IsPartialDefinition)
            .Select(property => new AccessorModel(property, component))
            .ToList();
    }

    internal static INamedTypeSymbol? ExplicitComponentOf(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ExplicitComponentAttribute
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is INamedTypeSymbol component)
                return component;

        return null;
    }

    private static IReadOnlyList<AccessorModel> FromAccessorClass(INamedTypeSymbol symbol)
    {
        var accessorClass = symbol.ContainingNamespace?
            .GetTypeMembers(AccessorEmitter.ClassNameOf(symbol.Name))
            .FirstOrDefault();

        if (accessorClass is null)
            return [];

        var methods = accessorClass.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsStatic).ToList();
        var setters = new HashSet<string>(methods
            .Where(method => method.Name.StartsWith("Set", System.StringComparison.Ordinal))
            .Select(method => method.Name.Substring(3)));

        return methods
            .Where(method => method.Name.StartsWith("Get", System.StringComparison.Ordinal) && method.Parameters.Length == 1)
            .Select(method => new AccessorModel(
                method.Name.Substring(3),
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                setters.Contains(method.Name.Substring(3))))
            .ToList();
    }

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
                includes.Add(new IncludeModel(included, IncludeKind.Archetype));
                continue;
            }

            if (name != ArchetypeNames.IncludeAttribute)
                continue;

            includes.Add(new IncludeModel(included, KindOf(included)));
        }

        return includes;
    }

    private static IncludeKind KindOf(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ArchetypeAttribute)
                return IncludeKind.Archetype;
        }

        return IncludeKind.Mixin;
    }
}
