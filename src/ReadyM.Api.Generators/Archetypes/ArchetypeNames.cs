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
    public const string Namespace = "ReadyM.SDK.Archetypes.Attributes";

    public const string ArchetypeAttribute = Namespace + ".ArchetypeAttribute";
    public const string MixinAttribute = Namespace + ".ArchetypeMixinAttribute";
    public const string TagAttribute = Namespace + ".TagAttribute";
    public const string IncludeAttribute = Namespace + ".IncludeAttribute";
    public const string IncludeArchetypeAttribute = Namespace + ".IncludeArchetypeAttribute";

    public const string EntityHandle = "global::ReadyM.SDK.Entity.EntityHandle";
    public const string ComponentSet = "global::ReadyM.SDK.Archetypes.ComponentSet";
    public const string Queryable = "global::ReadyM.SDK.Archetypes.IArchetypeQueryable";
    public const string Archetype = "global::ReadyM.SDK.Archetypes.IArchetype";
    public const string Mixin = "global::ReadyM.SDK.Archetypes.IArchetypeMixin";
    public const string Component = "global::Friflo.Engine.ECS.IComponent";
    public const string Tag = "global::Friflo.Engine.ECS.ITag";

    /// <summary>The component holding the values of a mixin, or an archetype's own accessors.</summary>
    public static string ComponentOf(INamedTypeSymbol declaration) => declaration.Name + "Component";

    /// <summary>The same component, named so it resolves from any namespace.</summary>
    public static string QualifiedComponentOf(INamedTypeSymbol declaration)
    {
        var ns = NamespaceOf(declaration);
        return ns.Length == 0 ? "global::" + ComponentOf(declaration) : $"global::{ns}.{ComponentOf(declaration)}";
    }

    public static string NamespaceOf(INamedTypeSymbol symbol)
        => symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToDisplayString();

    public static string FieldOf(string accessorName) => accessorName.ToLowerFirst();

    public static string TryGetOf(INamedTypeSymbol mixin) => "TryGet" + mixin.Name;

    public static string RequireOf(INamedTypeSymbol mixin) => "Require" + mixin.Name;

    public static string EnsureOf(INamedTypeSymbol mixin) => "Ensure" + mixin.Name;
}
