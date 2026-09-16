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
internal sealed class AccessorModel(string name, string type, bool hasSetter)
{
    public AccessorModel(IPropertySymbol property)
        : this(property.Name, property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), property.SetMethod is not null)
    {
    }

    public string Name { get; } = name;

    public string Type { get; } = type;

    public bool HasSetter { get; } = hasSetter;

    public string Field { get; } = ArchetypeNames.FieldOf(name);
}

/// <summary>One entry of an <c>[Include]</c> or <c>[IncludeArchetype]</c> on an archetype.</summary>
internal sealed class IncludeModel(INamedTypeSymbol type, IncludeKind kind, bool optional)
{
    public INamedTypeSymbol Type { get; } = type;

    public IncludeKind Kind { get; } = kind;

    public bool Optional { get; } = optional;

    public string TypeName { get; } = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string Parameter { get; } = type.Name.ToLowerFirst();

    /// <summary>The class a consumer goes through, wherever the declaration was compiled.</summary>
    public string Accessors { get; } = ArchetypeNames.QualifiedAccessorsOf(type);
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
    public IReadOnlyList<IncludeModel?> ComponentOwners()
    {
        var owners = new List<IncludeModel?>();

        Collect(this, null, owners, new HashSet<string>());
        return owners;
    }

    private static void Collect(
        DeclarationModel model,
        IncludeModel? owner,
        List<IncludeModel?> owners,
        HashSet<string> seen)
    {
        if (model.HasOwnComponent && seen.Add(model.QualifiedName))
            owners.Add(owner);

        foreach (var include in model.Includes)
        {
            if (include.Optional || include.Kind == IncludeKind.Tag)
                continue;

            if (include.Kind == IncludeKind.Archetype)
            {
                Collect(For(include.Type), include, owners, seen);
                continue;
            }

            var mixin = For(include.Type);

            if (mixin.HasOwnComponent && seen.Add(mixin.QualifiedName))
                owners.Add(include);
        }
    }

    /// <summary>
    /// Whether every component this shape carries is known at compile time, and in what order.
    /// </summary>
    /// <remarks>
    /// An optional mixin may be absent from a matching chunk, so its slot cannot be placed. An
    /// included archetype is fine: its components are flattened in the order its own set lists them.
    /// </remarks>
    public bool SupportsChunks
        => Includes.All(include => include.Kind == IncludeKind.Tag || !include.Optional)
           && Includes
               .Where(include => include.Kind == IncludeKind.Archetype)
               .All(include => For(include.Type).SupportsChunks)
           && ComponentOwners().Count > 0
           && Contributors().All(contributor => contributor.HasChunkAccessors);

    /// <summary>Every declaration whose component this shape carries, itself included.</summary>
    private IEnumerable<DeclarationModel> Contributors()
    {
        yield return this;

        foreach (var owner in ComponentOwners())
            if (owner is not null)
                yield return For(owner.Type);
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
            if (include.Kind == IncludeKind.Tag)
                continue;

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
        => symbol.DeclaringSyntaxReferences.IsEmpty
            ? FromAccessorClass(symbol)
            : symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(property => property.IsPartialDefinition)
                .Select(property => new AccessorModel(property))
                .ToList();

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
