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

    public const string EntityHandle = "global::ReadyM.SDK.Entity.EntityHandle";
    public const string ComponentSet = "global::ReadyM.SDK.Archetypes.ComponentSet";
    public const string Queryable = "global::ReadyM.SDK.Archetypes.IArchetypeQueryable";
    public const string Archetype = "global::ReadyM.SDK.Archetypes.IArchetype";
    public const string Mixin = "global::ReadyM.SDK.Archetypes.IArchetypeMixin";
    public const string Component = "global::Friflo.Engine.ECS.IComponent";

    public const string RawEntity = "global::Friflo.Engine.ECS.RawEntity";
    /// <summary>The class holding the GetEnumerator that puts a shape on the chunk path.</summary>
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

    /// <summary>The accessor class a consumer in any assembly goes through.</summary>
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
