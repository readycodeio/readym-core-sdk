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

        Narrows = component is not null && NarrowsFrom(property, Target(component, property));
        Cast = property.Type.WithNullableAnnotation(NullableAnnotation.None)
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        Accessibility = property.DeclaredAccessibility;
        IsNativeContainer = CSharpFieldSupportRegistry.FieldTypeSupportVisitor
                                .TryGetImpl(property.Type, false, out var support)
                            && support is NativeContainerFieldTypeSupportImplBase;
        IsIndex = property.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.IndexAttribute);
        IsPredictable = property.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.PredictableAttribute);

        // The compiler keeps the implementing part's comment over the declaration's, so the generated part only gets one
        // when the declaration has none.
        if (component is not null && DocComments.Of(property).Count == 0
                                  && Target(component, property) is { DeclaredAccessibility: not Accessibility.Private } target)
            InheritsDocFrom = $"{target.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{target.Name}";
    }

    public string Name { get; } = name;

    public string Type { get; } = type;

    public bool HasSetter { get; } = hasSetter;

    /// <summary>Whether [Index] marks this as the value the shape is found by.</summary>
    public bool IsIndex { get; }

    /// <summary>
    /// Whether [Predictable] lets a client guess at the value while it waits for the server. The guess is held
    /// beside the entity, so the getter reads it in front of the replicated value and the setter puts it there
    /// instead of writing one a client does not own.
    /// </summary>
    public bool IsPredictable { get; }

    /// <summary>The component member an undocumented value takes its doc comment from, as a cref.</summary>
    public string? InheritsDocFrom { get; }

    /// <summary>
    /// Whether the value is a native collection. A replicated component gives one of those a family
    /// of methods rather than a property, because handing back the collection itself would let a
    /// caller change it without the component noticing.
    /// </summary>
    public bool IsNativeContainer { get; }

    /// <summary>
    /// How the shape declared the value. A native collection stays private, so a mod reaches it
    /// only through its own members and cannot take the collection itself and change it unnoticed.
    /// </summary>
    public Accessibility Accessibility { get; } = Accessibility.Public;

    /// <summary>Whether the value is reachable from outside the shape under its own name.</summary>
    public bool IsExposed => Accessibility is not Accessibility.Private;

    /// <summary>The member holding the value: a generated field, or one of an explicit component.</summary>
    public string Field { get; } = field ?? ArchetypeNames.FieldOf(name);

    /// <summary>Set only while the declaration is in this compilation, which is when it can be checked.</summary>
    public IPropertySymbol? Declared { get; }

    public INamedTypeSymbol? Component { get; }

    /// <summary>
    /// What the component's Fields class calls this value. A generated component turns the shape's
    /// own name into the property, and one named by [ExplicitComponent] already had its own.
    /// </summary>
    public string FieldEntry => Component is null ? Name : Field;

    /// <summary>
    /// Whether the value sits in the component as a base type of the one the shape declared, which
    /// is how a shape gives a game's own storage the type a mod actually works with.
    /// </summary>
    public bool Narrows { get; }

    /// <summary>The declared type as an `as` operand, which cannot carry a nullable annotation.</summary>
    public string Cast { get; } = string.Empty;

    /// <summary>
    /// The read expression for this value, narrowed where the shape declared a derived type. A
    /// value that does not fit reads as null rather than throwing, which is what a shape sitting on
    /// storage shared by several kinds of entity needs.
    /// </summary>
    public string Read(string access) => Narrows ? $"{access} as {Cast}" : access;

    /// <summary>The field or property an explicit component holds this value in, matched by name.</summary>
    public static ISymbol? Target(INamedTypeSymbol component, IPropertySymbol property)
        => component.GetMembers()
            .FirstOrDefault(member => member is IFieldSymbol { IsStatic: false, IsConst: false } or IPropertySymbol { IsStatic: false }
                                      && string.Equals(member.Name, MemberNameOf(property), System.StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the shape declared a type the component's own member can be narrowed to.</summary>
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
    public ForwardModel(IMethodSymbol method, string? changes = null, string? collection = null, string? owns = null)
    {
        Changes = changes;
        Collection = collection;
        Owns = owns;
        Name = method.Name;
        ReturnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        Parameters = method.Parameters
            .Select(parameter => (
                Modifier: Passed(parameter.RefKind),
                Type: parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                parameter.Name))
            .ToList();
    }

    /// <summary>
    /// How the parameter is passed, which the forwarded member has to repeat both where it is
    /// declared and where it hands the argument on. TryGet takes its value out.
    /// </summary>
    private static string Passed(RefKind kind) => kind switch
    {
        RefKind.In => "in ",
        RefKind.Out => "out ",
        RefKind.Ref => "ref ",
        _ => string.Empty
    };

    public ForwardModel(IPropertySymbol property, string? owns = null)
    {
        Owns = owns;
        Name = property.Name;
        ReturnType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        Parameters = [];
        IsProperty = true;
    }

    /// <summary>For a member of a component that is being generated, and so has no symbol to read.</summary>
    public ForwardModel(
        string name,
        string returnType,
        IReadOnlyList<(string Modifier, string Type, string Name)> parameters,
        bool isProperty = false,
        string? changes = null,
        string? collection = null,
        string? owns = null)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
        IsProperty = isProperty;
        Changes = changes;
        Collection = collection;
        Owns = owns;
    }

    /// <summary>
    /// A property written with a body on the shape itself. Whatever includes or extends the shape reaches it through
    /// the shape's accessor class, which calls it on an instance of the shape.
    /// </summary>
    public static ForwardModel Computed(string name, string type, bool hasSetter)
        => new(name, type, [], isProperty: true) { IsComputed = true, HasSetter = hasSetter };

    public string Name { get; }

    public string ReturnType { get; }

    public IReadOnlyList<(string Modifier, string Type, string Name)> Parameters { get; }

    public bool IsProperty { get; }

    /// <summary>Whether this is a computed property of the shape rather than a member of its component.</summary>
    public bool IsComputed { get; private set; }

    /// <summary>Whether a computed property can be assigned too.</summary>
    public bool HasSetter { get; private set; }

    /// <summary>
    /// The Fields entry this member changes, for one that changes anything. A member reached off a
    /// component the shape borrows says nothing here: the entry is that component's to name.
    /// </summary>
    public string? Changes { get; }

    /// <summary>The token a caller reaches the collection through instead, for the message that says so.</summary>
    public string? Collection { get; }

    /// <summary>The collection this member belongs to, by the name the shape gave it.</summary>
    public string? Owns { get; }

    /// <summary>
    /// What the member is called where a prefix renames it.
    /// </summary>
    /// <remarks>
    /// A collection's name sits inside the member rather than at the front of it, so under the
    /// prefix Guest, GetNames is GetGuestNames. Putting the prefix in front would give GuestGetNames,
    /// which reads as a different verb and no longer matches the collection it belongs to.
    /// </remarks>
    public string Named(string prefix)
    {
        if (prefix.Length == 0)
            return Name;

        if (Owns is not { } collection)
            return prefix + Name;

        var at = DeclarationModel.IndexOfCollection(Name, collection);

        return at < 0 ? prefix + Name : Name.Substring(0, at) + prefix + Name.Substring(at);
    }

    public bool Returns => ReturnType != "void";

    /// <summary>The declaration, without the leading accessibility or the body.</summary>
    public string Signature => Declaration(Name);

    /// <summary>
    /// The same declaration under another name, which is what an extension needs when a prefix
    /// keeps two mods' members apart.
    /// </summary>
    public string Declaration(string name) => IsProperty
        ? $"{ReturnType} {name}"
        : $"{ReturnType} {name}({string.Join(", ", Parameters.Select(p => $"{p.Modifier}{p.Type} {p.Name}"))})";

    /// <summary>Whether anything has to go between the receiver and the forwarded arguments.</summary>
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
internal sealed class IncludeModel(
    INamedTypeSymbol type,
    IncludeKind kind,
    string prefix = "",
    Location? location = null,
    bool byIncludeArchetype = false)
{
    public INamedTypeSymbol Type { get; } = type;

    /// <summary>
    /// Whether [IncludeArchetype] named it rather than [Include], which a rule holds against what it actually is.
    /// </summary>
    public bool ByIncludeArchetype { get; } = byIncludeArchetype;

    /// <summary>Where the attribute naming it is written, for a diagnostic about what it brings.</summary>
    public Location Location { get; } = location ?? Location.None;

    public IncludeKind Kind { get; } = kind;

    /// <summary>
    /// Put in front of every member this include brings, for a name that would otherwise read
    /// oddly or clash with another include's. <c>ParentCell</c> turns <c>X</c> into
    /// <c>ParentCellX</c>.
    /// </summary>
    public string Prefix { get; } = prefix;

    /// <summary>What a member this include brings is called on the shape that took it.</summary>
    public string Named(string member) => Prefix + member;

    /// <summary>The same for a forwarded member, whose prefix may belong inside the name.</summary>
    public string Named(ForwardModel forward) => forward.Named(Prefix);

    public string TypeName { get; } = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string Parameter { get; } = type.Name.ToLowerFirst();

    /// <summary>The class a consumer goes through, wherever the declaration was compiled.</summary>
    public string Accessors { get; } = ArchetypeNames.QualifiedAccessorsOf(type);

    /// <summary>Whether what it brings goes over the wire, which decides what a write through it means.</summary>
    public bool IsReplicated { get; } = DeclarationModel.For(type).IsReplicated;

    /// <summary>
    /// Whether the included shape is hidden, which hides what it brings onto the shape that takes it.
    /// </summary>
    public bool IsHidden { get; } = DeclarationModel.IsHiddenType(type);
}

/// <summary>One entry in a shape's component set: a declaration's component, or its marker.</summary>
internal sealed class ComponentOwner(IncludeModel? include, bool marker)
{
    /// <summary>Null when it belongs to the declaration itself rather than to something it includes.</summary>
    public IncludeModel? Include { get; } = include;

    public bool IsMarker { get; } = marker;
}

/// <summary>
/// Mirrors <c>ReadyM.SDK.Attributes.Replication</c>: a generator cannot reference the SDK, and the
/// attribute argument arrives as that enum's index, so the order of these has to stay the same.
/// </summary>
internal enum Replication
{
    None,
    ToEcsOnly,
    ToGameOnly,
    Both,
    ServerAuthoritative,
    OwnershipBased
}

/// <summary>
/// Mirrors <c>ReadyM.SDK.Attributes.Delivery</c>: a generator cannot reference the SDK, and the attribute
/// argument arrives as that enum's index, so the order of these two has to stay the same.
/// </summary>
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
        Tags = ReadTags(symbol);
        (Networked, Replication, Delivery, StatesDelivery) = ReadReplication(symbol);
        CreateHandlers = ReadHandlers(symbol, ArchetypeNames.CreateHandlerAttribute);
        DeleteHandlers = ReadHandlers(symbol, ArchetypeNames.DeleteHandlerAttribute);
        Forwards = ReadForwards(symbol, ExplicitComponent, Collections).Concat(CollectionMembers()).ToList();
        Computed = ReadComputed(symbol);
    }

    /// <summary>
    /// The collection members of a component this shape is having generated. A component that
    /// already exists contributes through [ExplicitCollection] instead, and one that does not
    /// replicate holds a plain field with no members to carry.
    /// </summary>
    private IReadOnlyList<ForwardModel> CollectionMembers()
    {
        if (!EmitsComponent || !IsReplicated)
            return [];

        var found = new List<ForwardModel>();

        foreach (var accessor in Accessors)
            found.AddRange(NativeCollectionForwards.For(accessor, Name));

        return found;
    }

    public INamedTypeSymbol Symbol { get; }

    /// <summary>
    /// The access of the types generated beside the shape: public only when the shape and every type around it are,
    /// so a shape a mod keeps to itself does not have its plumbing exposed, or fail to compile for exposing it.
    /// </summary>
    public string Access
    {
        get
        {
            for (var type = Symbol; type is not null; type = type.ContainingType)
                if (type.DeclaredAccessibility != Accessibility.Public)
                    return "internal";

            return "public";
        }
    }

    /// <summary>Whether the shape is [EditorBrowsable(Never)], so what is generated for it is hidden as well.</summary>
    public bool IsHidden => IsHiddenType(Symbol);

    internal static bool IsHiddenType(INamedTypeSymbol symbol)
        => symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "System.ComponentModel.EditorBrowsableAttribute"
            && attribute.ConstructorArguments.Length == 1
            && attribute.ConstructorArguments[0].Value is int state
            && state == (int)System.ComponentModel.EditorBrowsableState.Never);

    public IReadOnlyList<AccessorModel> Accessors { get; }

    public IReadOnlyList<IncludeModel> Includes { get; }

    /// <summary>Members of the explicit component named by [ExplicitCollection].</summary>
    public IReadOnlyList<string> Collections { get; }

    /// <summary>What the shape asked to run as one of its entities is created.</summary>
    public IReadOnlyList<IMethodSymbol> CreateHandlers { get; }

    /// <summary>What the shape asked to run just before one of its entities goes.</summary>
    public IReadOnlyList<IMethodSymbol> DeleteHandlers { get; }

    /// <summary>Everything those members offer, mirrored onto this shape.</summary>
    public IReadOnlyList<ForwardModel> Forwards { get; }

    /// <summary>Public properties written with a body on this shape, which whatever includes or extends it gets too.</summary>
    public IReadOnlyList<ForwardModel> Computed { get; }

    /// Archetypes this shape is added to when they are created.
    /// <summary>Archetypes this shape is added to on create, and the prefix its members take.</summary>
    public IReadOnlyList<(INamedTypeSymbol Archetype, string Prefix)> Extends { get; }

    /// <summary>The tags every entity of this shape carries, named by [Tag].</summary>
    /// <remarks>
    /// Kept as symbols rather than names, because the generated code names them with typeof and the
    /// compiler then says so when a tag is misspelt or is not visible from the mod.
    /// </remarks>
    public IReadOnlyList<INamedTypeSymbol> Tags { get; }

    public string Namespace => ArchetypeNames.NamespaceOf(Symbol);

    public string Name => Symbol.Name;

    /// <summary>The struct header a generated part has to agree with.</summary>
    public string Header => Symbol.IsReadOnly ? $"readonly partial struct {Name}" : $"partial struct {Name}";

    /// <summary>The component named by [ExplicitComponent], or null when one is generated.</summary>
    public INamedTypeSymbol? ExplicitComponent { get; }

    /// <summary>
    /// What [Archetype(Networked = ...)] says, which is whether the archetype's entities get a network
    /// identity. Null for a mixin, and for an archetype whose attribute does not compile.
    /// </summary>
    public bool? Networked { get; }

    /// <summary>
    /// What the declaration states for Replication: the argument of [ArchetypeMixin], or [Archetype]'s
    /// named one. Null when it states none, which only an archetype can.
    /// </summary>
    public Replication? Replication { get; }

    /// <summary>
    /// Whether the declaration asks for its own values to cross the wire: a replicating Replication, on a
    /// networked archetype if it is one.
    /// </summary>
    public bool AsksToReplicate
        => Replication is { } stated && stated != Archetypes.Replication.None && (!IsArchetype || Networked == true);

    /// <summary>
    /// How its values travel between the game and the ECS, and who may write them, for a shape that
    /// replicates them. A local one is the mod's own business either way.
    /// </summary>
    public Replication? Propagation => AsksToReplicate ? Replication : null;

    /// <summary>How this shape's changes travel, when it replicates at all.</summary>
    public Delivery Delivery { get; }

    /// <summary>Whether the declaration names a Delivery at all, which only a shape sending values of its own may.</summary>
    public bool StatesDelivery { get; }

    /// <summary>
    /// Whether this shape's values reach the other side. A shape over a component it borrows takes
    /// that component's word for it; one with a component generated for it asks.
    /// </summary>
    /// <remarks>
    /// A shape holding no values replicates nothing whatever it asks for: there is no component to
    /// carry, and its presence travels with the entity instead.
    /// </remarks>
    public bool IsReplicated
        => ExplicitComponent is { } borrowed
            ? IsNetworkedComponent(borrowed)
            : AsksToReplicate && Accessors.Count > 0;

    /// <summary>Whether this shape is the one that tells the host about its component.</summary>
    /// <remarks>
    /// Only for a component the shape had generated. A component named by [ExplicitComponent]
    /// belongs to whoever declared it: it may already be one the host registered itself, and
    /// registering it again as a mod component is both wrong and, for a large one, refused.
    /// </remarks>
    public bool RegistersReplication => IsReplicated && EmitsComponent;

    public static bool IsNetworkedComponent(INamedTypeSymbol component)
        => component.AllInterfaces.Any(contract =>
               contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
               == ArchetypeNames.NetworkedComponent)
           // Declared in the same compilation, the interface is still to be generated
           || component.GetAttributes().Any(attribute
               => attribute.AttributeClass?.Name == "DeriveINetworkedComponentAttribute");

    /// <summary>
    /// What [Archetype] or [ArchetypeMixin] says about replicating: an archetype through named arguments, Networked
    /// on unless turned off, and a mixin through its required Replication.
    /// </summary>
    private static (bool? Networked, Replication? Replication, Delivery Delivery, bool StatesDelivery) ReadReplication(
        INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            var isArchetype = name == ArchetypeNames.ArchetypeAttribute;

            if (!isArchetype && name != ArchetypeNames.MixinAttribute)
                continue;

            bool? networked = isArchetype ? true : null;
            Replication? replication = !isArchetype
                                       && attribute.ConstructorArguments.Length > 0
                                       && attribute.ConstructorArguments[0].Value is int stated
                ? (Replication)stated
                : null;
            var delivery = Delivery.Reliable;
            var statesDelivery = false;

            foreach (var named in attribute.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Networked" when isArchetype && named.Value.Value is bool flag:
                        networked = flag;
                        break;
                    case "Replication" when isArchetype && named.Value.Value is int index:
                        replication = (Replication)index;
                        break;
                    case "Delivery" when named.Value.Value is int index:
                        delivery = (Delivery)index;
                        statesDelivery = true;
                        break;
                }
            }

            return (networked, replication, delivery, statesDelivery);
        }

        return (null, null, Delivery.Reliable, false);
    }

    /// <summary>
    /// What a component backing a shape does about replicating, in the shape's own terms: not sent, one of the five
    /// contracts it carries, or owner-written, which is how the server treats a networked component that carries none.
    /// </summary>
    public static Replication ReplicationOf(INamedTypeSymbol component)
    {
        if (!IsNetworkedComponent(component))
            return Archetypes.Replication.None;

        foreach (var value in new[]
                 {
                     Archetypes.Replication.ToEcsOnly, Archetypes.Replication.ToGameOnly, Archetypes.Replication.Both,
                     Archetypes.Replication.ServerAuthoritative, Archetypes.Replication.OwnershipBased
                 })
        {
            var contract = PropagationContracts.Of(value);

            if (component.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == contract))
                return value;
        }

        return Archetypes.Replication.OwnershipBased;
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
        var taken = new HashSet<string>(Forwards.Concat(Computed).Select(forward => forward.Signature));

        // Keyed on what the member ends up called, so a prefix is what keeps two of the same name apart.

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

            foreach (var forward in included.Forwards.Concat(included.Computed)
                         .Where(forward => taken.Add(forward.Declaration(include.Named(forward.Name)))))
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

                foreach (var accessor in included.Accessors
                             .Where(accessor => taken.Add(include.Named(accessor.Name))))
                    into.Add((include, accessor));

                Flatten(included, into, taken);
                continue;
            }

            foreach (var accessor in ReadAccessors(include.Type)
                         .Where(accessor => taken.Add(include.Named(accessor.Name))))
                into.Add((include, accessor));
        }
    }

    /// <summary>
    /// Every name this shape would expose twice, from two different places: its own members and
    /// everything its includes bring, under the names the flattening above gives them.
    /// </summary>
    /// <remarks>
    /// The flattening keeps the first of two members of one name and drops the rest, so a clash would
    /// otherwise send every call to whichever include happened to be listed first. The same member
    /// reached twice is no clash: a shape re-including a mixin the archetype it builds on already has
    /// gets that mixin's members twice, and they are one member.
    /// </remarks>
    public IReadOnlyList<NameClash> NameClashes()
    {
        var first = new Dictionary<string, (string Origin, string From)>();
        var clashes = new List<NameClash>();

        void Add(string key, string shown, string origin, string from, Location at)
        {
            if (!first.TryGetValue(key, out var earlier))
                first[key] = (origin, from);
            else if (earlier.Origin != origin)
                clashes.Add(new NameClash(shown, earlier.From, from, at));
        }

        var own = Symbol.Locations.FirstOrDefault() ?? Location.None;

        foreach (var accessor in Accessors)
        {
            var at = Symbol.GetMembers(accessor.Name).FirstOrDefault()?.Locations.FirstOrDefault() ?? own;
            Add(PropertyKey(accessor.Name), accessor.Name, $"{QualifiedName}|{accessor.Name}", $"'{Name}' itself", at);
        }

        foreach (var forward in Forwards.Concat(Computed))
            Add(ForwardKey(forward, forward.Name), forward.Name, $"{QualifiedName}|{ForwardKey(forward, forward.Name)}",
                $"'{Name}' itself", own);

        Walk(this, null);
        return clashes;

        void Walk(DeclarationModel model, IncludeModel? through)
        {
            foreach (var include in model.Includes)
            {
                // A member arriving through an included archetype is reported where that archetype is included
                var at = (through ?? include).Location;
                var included = For(include.Type);
                var from = $"'{include.Type.Name}'";
                var accessors = include.Kind == IncludeKind.Archetype ? included.Accessors : ReadAccessors(include.Type);

                foreach (var accessor in accessors)
                {
                    var name = include.Named(accessor.Name);
                    Add(PropertyKey(name), name, $"{included.QualifiedName}|{include.Prefix}|{accessor.Name}", from, at);
                }

                foreach (var forward in included.Forwards.Concat(included.Computed))
                {
                    var name = include.Named(forward);
                    Add(ForwardKey(forward, name), name, $"{included.QualifiedName}|{include.Prefix}|{ForwardKey(forward, forward.Name)}",
                        from, at);
                }

                if (include.Kind == IncludeKind.Archetype)
                    Walk(included, through ?? include);
            }
        }
    }

    // A property and a property-like forward share one name space; a method is told apart by what it takes,
    // so overloads are not clashes
    private static string PropertyKey(string name) => "P:" + name;

    private static string ForwardKey(ForwardModel forward, string name)
        => forward.IsProperty
            ? PropertyKey(name)
            : $"M:{name}({string.Join(",", forward.Parameters.Select(p => p.Modifier + p.Type))})";


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

    /// <summary>
    /// The shape's computed properties: written with a body rather than declared partial, public, and read through a
    /// public getter. A shape compiled into another assembly lists them in its accessor class instead, where nothing
    /// generated can be taken for one.
    /// </summary>
    private static IReadOnlyList<ForwardModel> ReadComputed(INamedTypeSymbol symbol)
    {
        if (symbol.DeclaringSyntaxReferences.IsEmpty)
            return ComputedFromAccessorClass(symbol);

        return symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => property is
            {
                IsStatic: false,
                IsIndexer: false,
                IsPartialDefinition: false,
                PartialDefinitionPart: null,
                DeclaredAccessibility: Accessibility.Public,
                ExplicitInterfaceImplementations.Length: 0,
                GetMethod.DeclaredAccessibility: Accessibility.Public
            })
            .Select(property => ForwardModel.Computed(
                property.Name,
                property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                property.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false }))
            .ToList();
    }

    private static IReadOnlyList<ForwardModel> ComputedFromAccessorClass(INamedTypeSymbol symbol)
    {
        var computed = symbol.ContainingNamespace?
            .GetTypeMembers(AccessorEmitter.ClassNameOf(symbol.Name))
            .FirstOrDefault()?
            .GetTypeMembers(AccessorEmitter.ComputedClassName)
            .FirstOrDefault();

        if (computed is null)
            return [];

        var methods = computed.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsStatic).ToList();
        var setters = new HashSet<string>(methods
            .Where(method => method.Parameters.Length == 2 && method.Name.StartsWith("Set", System.StringComparison.Ordinal))
            .Select(method => method.Name.Substring(3)));

        return methods
            .Where(method => method.Parameters.Length == 1)
            .Select(method => ForwardModel.Computed(
                method.Name,
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                setters.Contains(method.Name)))
            .ToList();
    }

    private static IReadOnlyList<IMethodSymbol> ReadHandlers(INamedTypeSymbol symbol, string attributeName)
        => [.. symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method.GetAttributes().Any(attribute
                => attribute.AttributeClass?.ToDisplayString() == attributeName))];

    private static IReadOnlyList<string> CollectionsOf(INamedTypeSymbol symbol)
        => symbol.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ExplicitCollectionAttribute)
            .Select(attribute => attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string : null)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();

    private static IReadOnlyList<INamedTypeSymbol> ReadTags(INamedTypeSymbol symbol)
    {
        var tags = new List<INamedTypeSymbol>();

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != ArchetypeNames.TagAttribute
                || attribute.ConstructorArguments.Length == 0
                || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol tag)
                continue;

            if (!tags.Contains(tag, SymbolEqualityComparer.Default))
                tags.Add(tag);
        }

        return tags;
    }

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

        // Only a networked component has a Fields table for a change to be asked about, and only a
        // shape over one replicates, so a local component's members carry no rule to keep.
        var replicates = IsNetworkedComponent(component);

        foreach (var member in component.GetMembers())
        {
            if (collections.FirstOrDefault(collection => Forwardable(member, collection)) is not { } owns)
                continue;

            if (member is IMethodSymbol method)
            {
                var changed = replicates
                    ? collections.FirstOrDefault(collection => Changes(method.Name, collection))
                    : null;

                forwards.Add(changed is null
                    ? new ForwardModel(method, owns: owns)
                    : new ForwardModel(method, changed, $"{symbol.Name}.{ValuesEmitter.ClassName}.{changed}", owns));
            }
            else if (member is IPropertySymbol property)
                forwards.Add(new ForwardModel(property, owns));
        }

        return forwards;
    }

    /// <summary>
    /// Whether a member of a component changes the named collection rather than reading it. The
    /// verbs are the ones the field support puts there, so this reads the same surface it wrote.
    /// </summary>
    private static bool Changes(string member, string collection)
    {
        foreach (var verb in new[] { "Set", "Add", "Insert", "RemoveAt", "Remove", "Clear" })
            if (member == verb + collection)
                return true;

        return false;
    }

    /// <summary>Whether a member of the component is part of the named collection's surface.</summary>
    internal static bool Forwardable(ISymbol member, string collection)
        => !member.IsStatic
           && member.DeclaredAccessibility == Accessibility.Public
           && NamesCollection(member.Name, collection)
           && !IsPlumbing(member.Name)
           && member is IMethodSymbol { MethodKind: MethodKind.Ordinary } or IPropertySymbol { IsIndexer: false };

    /// <summary>
    /// Whether the member's name carries the collection's name as a word of its own, which is what
    /// the field support does to it: a verb in front, a role behind, and the name itself between.
    /// </summary>
    /// <remarks>
    /// Looking for the name anywhere in the member also catches a value of the component whose own
    /// name merely starts with it. A component holding a FaceMorph dictionary alongside a
    /// FaceMorphsSourcePath string had the string forwarded as part of the collection, which then
    /// collided with the shape's own declaration of it. Naming the members outright would be
    /// tighter still, and would drop whatever a hand-written component calls its own.
    /// </remarks>
    private static bool NamesCollection(string member, string collection)
        => IndexOfCollection(member, collection) >= 0;

    /// <summary>Where the collection's name sits in the member, or -1 when it does not.</summary>
    internal static int IndexOfCollection(string member, string collection)
    {
        for (var at = member.IndexOf(collection, System.StringComparison.Ordinal);
             at >= 0;
             at = member.IndexOf(collection, at + 1, System.StringComparison.Ordinal))
        {
            var after = at + collection.Length;

            if (StartsWord(member, at) && (after == member.Length || !char.IsLower(member[after])))
                return at;
        }

        return -1;
    }

    /// <summary>
    /// A word starts at the front of the name, or where something other than a capital gives way to
    /// one. Anything else is the middle of a longer word, which is not this collection.
    /// </summary>
    private static bool StartsWord(string member, int at) => at == 0 || !char.IsUpper(member[at - 1]);

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

    /// <summary>Names a networked component uses to talk to the replication layer.</summary>
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

            var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();

            if (name == ArchetypeNames.IncludeArchetypeAttribute)
            {
                includes.Add(new IncludeModel(included, IncludeKind.Archetype, location: location, byIncludeArchetype: true));
                continue;
            }

            if (name != ArchetypeNames.IncludeAttribute)
                continue;

            var prefix = attribute.ConstructorArguments.Length > 1
                ? attribute.ConstructorArguments[1].Value as string
                : null;

            includes.Add(new IncludeModel(included, KindOf(included), prefix ?? string.Empty, location));
        }

        return includes;
    }

    internal static IncludeKind KindOf(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ArchetypeAttribute)
                return IncludeKind.Archetype;
        }

        return IncludeKind.Mixin;
    }
}