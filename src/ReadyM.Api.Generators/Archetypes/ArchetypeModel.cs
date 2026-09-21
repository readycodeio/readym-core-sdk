using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Derive.CSharp;
using ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

namespace ReadyM.Api.Generators.Archetypes;

internal enum IncludeKind
{
    Mixin,
    Archetype
}

/// One partial property an author declared on a mixin or archetype.
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

        Narrows = component is not null && NarrowsFrom(property, Target(component, property));
        Cast = property.Type.WithNullableAnnotation(NullableAnnotation.None)
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        Accessibility = property.DeclaredAccessibility;
        IsNativeContainer = CSharpFieldSupportRegistry.FieldTypeSupportVisitor
            .TryGetImpl(property.Type, false, out var support)
            && support is NativeContainerFieldTypeSupportImplBase;
        IsIndex = property.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.IndexAttribute);
    }

    public string Name { get; } = name;

    public string Type { get; } = type;

    public bool HasSetter { get; } = hasSetter;

    /// <summary>Whether [Index] marks this as the value the shape is found by.</summary>
    public bool IsIndex { get; }

    /// Whether the value is a native collection. A replicated component gives one of those a family
    /// of methods rather than a property, because handing back the collection itself would let a
    /// caller change it without the component noticing.
    public bool IsNativeContainer { get; }

    /// How the shape declared the value. A native collection stays private, so a mod reaches it
    /// only through its own members and cannot take the collection itself and change it unnoticed.
    public Accessibility Accessibility { get; } = Accessibility.Public;

    /// Whether the value is reachable from outside the shape under its own name.
    public bool IsExposed => Accessibility is not Accessibility.Private;

    /// <summary>The member holding the value: a generated field, or one of an explicit component.</summary>
    public string Field { get; } = field ?? ArchetypeNames.FieldOf(name);

    /// <summary>Set only while the declaration is in this compilation, which is when it can be checked.</summary>
    public IPropertySymbol? Declared { get; }

    public INamedTypeSymbol? Component { get; }

    /// Whether the value sits in the component as a base type of the one the shape declared, which
    /// is how a shape gives a game's own storage the type a mod actually works with.
    public bool Narrows { get; }

    /// The declared type as an `as` operand, which cannot carry a nullable annotation.
    public string Cast { get; } = string.Empty;

    /// The read expression for this value, narrowed where the shape declared a derived type. A
    /// value that does not fit reads as null rather than throwing, which is what a shape sitting on
    /// storage shared by several kinds of entity needs.
    public string Read(string access) => Narrows ? $"{access} as {Cast}" : access;

    /// <summary>The field or property an explicit component holds this value in, matched by name.</summary>
    public static ISymbol? Target(INamedTypeSymbol component, IPropertySymbol property)
        => component.GetMembers()
            .FirstOrDefault(member => member is IFieldSymbol { IsStatic: false, IsConst: false } or IPropertySymbol { IsStatic: false }
                && string.Equals(member.Name, MemberNameOf(property), System.StringComparison.OrdinalIgnoreCase));

    /// Whether the shape declared a type the component's own member can be narrowed to.
    public static bool NarrowsFrom(IPropertySymbol property, ISymbol? target)
    {
        if (TypeOf(target) is not { } held)
            return false;

        var declared = property.Type;

        if (declared.IsValueType || held.IsValueType
            || SymbolEqualityComparer.Default.Equals(declared.WithNullableAnnotation(NullableAnnotation.None),
                held.WithNullableAnnotation(NullableAnnotation.None)))
            return false;

        return Derives(declared, held);
    }

    public static ITypeSymbol? TypeOf(ISymbol? target) => target switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        _ => null
    };

    private static bool Derives(ITypeSymbol declared, ITypeSymbol held)
    {
        var bare = held.WithNullableAnnotation(NullableAnnotation.None);

        if (held.TypeKind == TypeKind.Interface)
            return declared.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, bare));

        for (var current = declared.BaseType; current is not null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, bare))
                return true;

        return false;
    }

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

/// <summary>One member of an explicit component, forwarded onto the shape unchanged.</summary>
internal sealed class ForwardModel
{
    public ForwardModel(IMethodSymbol method)
    {
        Name = method.Name;
        ReturnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        Parameters = method.Parameters
            .Select(parameter => (
                Modifier: parameter.RefKind == RefKind.In ? "in " : string.Empty,
                Type: parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                parameter.Name))
            .ToList();
    }

    public ForwardModel(IPropertySymbol property)
    {
        Name = property.Name;
        ReturnType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        Parameters = [];
        IsProperty = true;
    }

    /// For a member of a component that is being generated, and so has no symbol to read.
    public ForwardModel(
        string name,
        string returnType,
        IReadOnlyList<(string Modifier, string Type, string Name)> parameters,
        bool isProperty = false)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
        IsProperty = isProperty;
    }

    public string Name { get; }

    public string ReturnType { get; }

    public IReadOnlyList<(string Modifier, string Type, string Name)> Parameters { get; }

    public bool IsProperty { get; }

    public bool Returns => ReturnType != "void";

    /// <summary>The declaration, without the leading accessibility or the body.</summary>
    public string Signature => Declaration(Name);

    /// The same declaration under another name, which is what an extension needs when a prefix
    /// keeps two mods' members apart.
    public string Declaration(string name) => IsProperty
        ? $"{ReturnType} {name}"
        : $"{ReturnType} {name}({string.Join(", ", Parameters.Select(p => $"{p.Modifier}{p.Type} {p.Name}"))})";

    /// Whether anything has to go between the receiver and the forwarded arguments.
    public string Separator => Parameters.Count == 0 ? "" : ", ";

    public string Arguments => string.Join(", ", Parameters.Select(p => $"{p.Modifier}{p.Name}"));

    /// <summary>The same parameters, after whatever the caller has to pass first.</summary>
    public string ParametersAfter(string leading)
    {
        var rest = Parameters.Select(p => $"{p.Modifier}{p.Type} {p.Name}");
        return string.Join(", ", new[] { leading }.Concat(rest));
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
    /// Null when it belongs to the declaration itself rather than to something it includes.
    public IncludeModel? Include { get; } = include;

    public bool IsMarker { get; } = marker;
}

/// How a replicated shape's changes reach the other side. Mirrors
/// <c>ReadyM.SDK.Attributes.Delivery</c>: a generator cannot reference the SDK, and the attribute
/// argument arrives as that enum's index, so the order of these two has to stay the same.
internal enum Delivery
{
    Reliable,
    Unreliable
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
        Collections = CollectionsOf(symbol);
        Accessors = ReadAccessors(symbol);
        Includes = ReadIncludes(symbol);
        Extends = ReadExtends(symbol);
        (HasReplicatedAttribute, Delivery) = ReadReplication(symbol);
        Forwards = ReadForwards(symbol, ExplicitComponent, Collections).Concat(CollectionMembers()).ToList();
    }

    /// The collection members of a component this shape is having generated. A component that
    /// already exists contributes through [ExplicitCollection] instead, and one that does not
    /// replicate holds a plain field with no members to carry.
    private IReadOnlyList<ForwardModel> CollectionMembers()
    {
        if (!EmitsComponent || !IsReplicated)
            return [];

        var found = new List<ForwardModel>();

        foreach (var accessor in Accessors)
            found.AddRange(NativeCollectionForwards.For(accessor));

        return found;
    }

    public INamedTypeSymbol Symbol { get; }

    public IReadOnlyList<AccessorModel> Accessors { get; }

    public IReadOnlyList<IncludeModel> Includes { get; }

    /// <summary>Members of the explicit component named by [ExplicitCollection].</summary>
    public IReadOnlyList<string> Collections { get; }

    /// <summary>Everything those members offer, mirrored onto this shape.</summary>
    public IReadOnlyList<ForwardModel> Forwards { get; }

    /// Archetypes this shape is added to when they are created.
    /// <summary>Archetypes this shape is added to on create, and the prefix its members take.</summary>
    public IReadOnlyList<(INamedTypeSymbol Archetype, string Prefix)> Extends { get; }

    public string Namespace => ArchetypeNames.NamespaceOf(Symbol);

    public string Name => Symbol.Name;

    /// The struct header a generated part has to agree with.
    public string Header => Symbol.IsReadOnly ? $"readonly partial struct {Name}" : $"partial struct {Name}";

    /// <summary>The component named by [ExplicitComponent], or null when one is generated.</summary>
    public INamedTypeSymbol? ExplicitComponent { get; }

    /// Whether [Replicated] is on the declaration. Kept apart from <see cref="IsReplicated"/> so a
    /// rule can tell asking to replicate from replicating because the component already does.
    public bool HasReplicatedAttribute { get; }

    /// How this shape's changes travel, when it replicates at all.
    public Delivery Delivery { get; }

    /// Whether this shape's values reach the other side. A shape says so with [Replicated], or says
    /// nothing and inherits it from a component that is already networked.
    /// <remarks>
    /// A shape holding no values replicates nothing whatever it asks for: there is no component to
    /// carry, and its presence travels with the entity instead.
    /// </remarks>
    public bool IsReplicated
        => (HasReplicatedAttribute || (ExplicitComponent is { } c && IsNetworkedComponent(c)))
           && (ExplicitComponent is not null || Accessors.Count > 0);

    /// Whether this shape is the one that tells the host about its component.
    /// <remarks>
    /// Only for a component the shape had generated. A component named by [ExplicitComponent]
    /// belongs to whoever declared it: it may already be one the host registered itself, and
    /// registering it again as a mod component is both wrong and, for a large one, refused.
    /// </remarks>
    public bool RegistersReplication => IsReplicated && EmitsComponent;

    public static bool IsNetworkedComponent(INamedTypeSymbol component)
        => component.AllInterfaces.Any(contract =>
            contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            == ArchetypeNames.NetworkedComponent);

    private static (bool Present, Delivery Delivery) ReadReplication(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != ArchetypeNames.ReplicatedAttribute)
                continue;

            var delivery = attribute.ConstructorArguments.Length > 0
                           && attribute.ConstructorArguments[0].Value is int index
                ? (Delivery)index
                : Delivery.Reliable;

            return (true, delivery);
        }

        return (false, Delivery.Reliable);
    }

    /// <summary>The accessor [Index] marks, for a shape whose component is generated.</summary>
    public AccessorModel? IndexAccessor => Accessors.FirstOrDefault(accessor => accessor.IsIndex);

    /// <summary>
    /// What this shape is found by, taken from [Index] or from the component it already has.
    /// </summary>
    public string? IndexedBy => IndexAccessor?.Type ?? ExplicitValueType();

    private string? ExplicitValueType()
    {
        if (ExplicitComponent is null)
            return null;

        foreach (var contract in ExplicitComponent.AllInterfaces)
            if (contract.ConstructedFrom.ToDisplayString() == "Friflo.Engine.ECS.IIndexedComponent<TValue>"
                && contract.TypeArguments.Length == 1)
                return contract.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return null;
    }

    /// Only a declaration with accessors of its own needs a component to keep them in.
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

    /// <summary>Whether the author declared this shape a scope, by naming IScope on their own part.</summary>
    public bool IsScope => Symbol.AllInterfaces.Any(contract => contract.ToDisplayString() == ArchetypeNames.ScopeMarker);

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

    /// <summary>The forwards a mixin contributes, flattened onto whatever includes it.</summary>
    public IReadOnlyList<(IncludeModel Include, ForwardModel Forward)> FlattenedForwards()
    {
        var flattened = new List<(IncludeModel, ForwardModel)>();
        var taken = new HashSet<string>(Forwards.Select(forward => forward.Signature));

        FlattenForwards(this, flattened, taken);
        return flattened;
    }

    private static void FlattenForwards(
        DeclarationModel model,
        List<(IncludeModel, ForwardModel)> into,
        HashSet<string> taken)
    {
        foreach (var include in model.Includes)
        {
            var included = For(include.Type);

            foreach (var forward in included.Forwards.Where(forward => taken.Add(forward.Signature)))
                into.Add((include, forward));

            if (include.Kind == IncludeKind.Archetype)
                FlattenForwards(included, into, taken);
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

    private static IReadOnlyList<string> CollectionsOf(INamedTypeSymbol symbol)
        => symbol.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ExplicitCollectionAttribute)
            .Select(attribute => attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string : null)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();

    private static IReadOnlyList<(INamedTypeSymbol, string)> ReadExtends(INamedTypeSymbol symbol)
    {
        var extends = new List<(INamedTypeSymbol, string)>();

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != ArchetypeNames.ExtendsAttribute
                || attribute.ConstructorArguments.Length == 0
                || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol archetype)
                continue;

            var prefix = attribute.ConstructorArguments.Length > 1
                ? attribute.ConstructorArguments[1].Value as string
                : null;

            extends.Add((archetype, prefix ?? string.Empty));
        }

        return extends;
    }

    /// <summary>
    /// What a named collection offers, taken from the component itself so the shape gains exactly
    /// the same members. The plumbing a networked component carries alongside them is left out: it
    /// belongs to replication, not to anyone holding the shape.
    /// </summary>
    private static IReadOnlyList<ForwardModel> ReadForwards(
        INamedTypeSymbol symbol,
        INamedTypeSymbol? component,
        IReadOnlyList<string> collections)
    {
        if (component is null || collections.Count == 0)
            return [];

        var forwards = new List<ForwardModel>();

        foreach (var member in component.GetMembers())
        {
            if (!collections.Any(collection => Forwardable(member, collection)))
                continue;

            if (member is IMethodSymbol method)
                forwards.Add(new ForwardModel(method));
            else if (member is IPropertySymbol property)
                forwards.Add(new ForwardModel(property));
        }

        return forwards;
    }

    /// <summary>Whether a member of the component is part of the named collection's surface.</summary>
    internal static bool Forwardable(ISymbol member, string collection)
        => !member.IsStatic
           && member.DeclaredAccessibility == Accessibility.Public
           && member.Name.IndexOf(collection, System.StringComparison.Ordinal) >= 0
           && !IsPlumbing(member.Name)
           && member is IMethodSymbol { MethodKind: MethodKind.Ordinary } or IPropertySymbol { IsIndexer: false };

    /// <summary>Where [ExplicitCollection] names this collection, so a report lands on it.</summary>
    internal static Location LocationOfCollection(INamedTypeSymbol symbol, string collection, Location fallback)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ExplicitCollectionAttribute
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value as string == collection)
                return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback;

        return fallback;
    }

    /// Names a networked component uses to talk to the replication layer.
    internal static bool IsPlumbing(string name)
        => name.IndexOf('_') >= 0
           || name.EndsWith("NotifyChanged", System.StringComparison.Ordinal)
           || name.EndsWith("LastChanged", System.StringComparison.Ordinal);

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

        // A forwarded collection member can read as Get<Name>/Set<Name>, which is the shape an
        // accessor has too. The attribute naming it survives into metadata, so it tells them apart.
        var collections = CollectionsOf(symbol);

        var methods = accessorClass.GetMembers().OfType<IMethodSymbol>()
            .Where(method => method.IsStatic)
            .Where(method => !collections.Any(c => method.Name.IndexOf(c, System.StringComparison.Ordinal) >= 0))
            .ToList();
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
