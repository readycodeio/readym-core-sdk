using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// The naming rules every archetype generator agrees on.
/// </summary>
/// <remarks>
/// Roslyn generators cannot see each other's output, and an archetype needs the component a mixin
/// declared elsewhere in the same compilation will get. Deriving the name from the declaration
/// rather than looking it up is what removes that dependency.
/// </remarks>
internal static class ArchetypeNames
{
    public const string Namespace = "ReadyM.SDK.Attributes";

    public const string ArchetypeAttribute = Namespace + ".ArchetypeAttribute";
    public const string MixinAttribute = Namespace + ".ArchetypeMixinAttribute";
    public const string IncludeAttribute = Namespace + ".IncludeAttribute";
    public const string IncludeArchetypeAttribute = Namespace + ".IncludeArchetypeAttribute";
    public const string ExplicitComponentAttribute = Namespace + ".ExplicitComponentAttribute";
    public const string ExplicitMemberAttribute = Namespace + ".ExplicitMemberAttribute";
    public const string ExplicitCollectionAttribute = Namespace + ".ExplicitCollectionAttribute";
    public const string ExtendsAttribute = Namespace + ".ExtendsAttribute";
    public const string IndexAttribute = Namespace + ".IndexAttribute";
    public const string ReplicatedAttribute = Namespace + ".ReplicatedAttribute";

    public const string EntityHandle = "global::ReadyM.SDK.Entities.EntityHandle";
    public const string ComponentSet = "global::ReadyM.SDK.Archetypes.ComponentSet";
    public const string Queryable = "global::ReadyM.SDK.Archetypes.IArchetypeQueryable";
    public const string Shape = "global::ReadyM.SDK.Archetypes.IEntityShape";
    public const string Archetype = "global::ReadyM.SDK.Archetypes.IArchetype";
    public const string Mixin = "global::ReadyM.SDK.Archetypes.IArchetypeMixin";
    public const string Component = "global::Friflo.Engine.ECS.IComponent";
    public const string Registry = "global::ReadyM.SDK.Archetypes.ArchetypeRegistry";
    public const string Indexed = "global::ReadyM.SDK.Archetypes.IIndexed";
    
    /// Matched against a symbol's ToDisplayString, which carries no global:: prefix.
    public const string ScopeMarker = "ReadyM.SDK.Archetypes.IScope";
    public const string Scope = "global::ReadyM.SDK.Archetypes.Scope";
    public const string IndexRegistry = "global::ReadyM.SDK.Archetypes.IndexRegistry";
    public const string ReplicationRegistry = "global::ReadyM.SDK.Archetypes.ReplicationRegistry";
    public const string NativeInitRegistry = "global::ReadyM.SDK.Archetypes.NativeInitRegistry";

    public const string WriteKind = "global::ReadyM.SDK.Entities.WriteKind";

    public const string Value = "global::ReadyM.SDK.Archetypes.Access.Value";

    public const string ShapeToken = "global::ReadyM.SDK.Archetypes.Access.Shape";
    public const string Delivery = "global::" + Namespace + ".Delivery";
    public const string IndexedComponent = "global::Friflo.Engine.ECS.IIndexedComponent";
    public const string NativeInit = "global::ReadyM.Api.ECS.Components.INativeInit";
    public const string AllocatorKind = "global::Yooni.Native.LowLevel.AllocatorKind";
    public const string NetworkedComponent = "global::ReadyM.Api.Multiplayer.ECS.Components.INetworkedComponent";

    public const string RawEntity = "global::Friflo.Engine.ECS.RawEntity";
    /// The class holding the GetEnumerator that puts a shape on the chunk path.
    public static string ChunkQueryOf(INamedTypeSymbol declaration) => declaration.Name + "ChunkQuery";

    /// <summary>The chunk-backed twin of a declaration.</summary>
    public static string ViewOf(INamedTypeSymbol declaration) => declaration.Name + "View";

    public static string QualifiedViewOf(INamedTypeSymbol declaration)
    {
        var ns = NamespaceOf(declaration);
        var name = ViewOf(declaration);
        return ns.Length == 0 ? "global::" + name : $"global::{ns}.{name}";
    }

    /// <summary>
    /// The component that says an entity was created as this archetype, or as one including it.
    /// </summary>
    /// <remarks>
    /// A component rather than a tag because the server has no tags yet. It is generated and
    /// internal, so swapping it for one later is invisible to a mod.
    /// </remarks>
    public static string MarkerOf(INamedTypeSymbol declaration) => declaration.Name + "ArchetypeMarker";

    public static string QualifiedMarkerOf(INamedTypeSymbol declaration)
    {
        var ns = NamespaceOf(declaration);
        var name = MarkerOf(declaration);
        return ns.Length == 0 ? "global::" + name : $"global::{ns}.{name}";
    }

    /// <summary>The component holding the values of a mixin, or an archetype's own accessors.</summary>
    public static string ComponentOf(INamedTypeSymbol declaration) => declaration.Name + "Component";

    /// <summary>The same component, named so it resolves from any namespace.</summary>
    public static string QualifiedComponentOf(INamedTypeSymbol declaration)
    {
        var ns = NamespaceOf(declaration);
        return ns.Length == 0 ? "global::" + ComponentOf(declaration) : $"global::{ns}.{ComponentOf(declaration)}";
    }

    /// The accessor class a consumer in any assembly goes through.
    public static string QualifiedAccessorsOf(INamedTypeSymbol declaration)
    {
        var ns = NamespaceOf(declaration);
        var name = declaration.Name + "Accessors";
        return ns.Length == 0 ? "global::" + name : $"global::{ns}.{name}";
    }

    public static string NamespaceOf(INamedTypeSymbol symbol)
        => symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToDisplayString();

    /// <summary>
    /// A hint name has to be unique across everything one generator emits, and two declarations in
    /// different namespaces may share a simple name, so the namespace is part of it.
    /// </summary>
    public static string HintOf(INamedTypeSymbol symbol, string kind)
    {
        var ns = NamespaceOf(symbol);
        return ns.Length == 0 ? $"{symbol.Name}.{kind}.g.cs" : $"{ns}.{symbol.Name}.{kind}.g.cs";
    }

    public static string FieldOf(string accessorName) => accessorName.ToLowerFirst();



}
