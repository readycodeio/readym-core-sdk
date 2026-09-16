using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// The chunk fast path's types, found in the server SDK rather than spelled out here.
/// </summary>
/// <remarks>
/// Written this way because the first version hard-coded the namespace, and moving those types one
/// namespace deeper turned the whole fast path off without a word: every shape quietly fell back to
/// walking identities and only a test counting interop calls noticed. Looking the types up by name
/// inside the assembly means a move cannot desync them, and a reference that is present without them
/// is reported rather than ignored.
/// </remarks>
internal sealed class ChunkNames
{
    public const string ServerAssembly = "ReadyM.SDK.Server";

    private ChunkNames(string chunks, string entity)
    {
        ComponentChunk = $"global::{chunks}.ComponentChunk";
        ChunkSlot = $"global::{chunks}.ChunkSlot";
        ChunkView = $"global::{chunks}.IArchetypeChunkView";
        ChunkQuery = $"global::{chunks}.ChunkQuery";
        ChunkViews = $"global::{chunks}.ChunkViews";
        EntityQuery = $"global::{entity}.EntityQuery";
    }

    public string ComponentChunk { get; }

    public string ChunkSlot { get; }

    public string ChunkView { get; }

    public string ChunkQuery { get; }

    public string ChunkViews { get; }

    public string EntityQuery { get; }

    /// <summary>Whether a compilation could use the fast path at all.</summary>
    public static bool ServerReferenced(Compilation compilation)
    {
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            if (assembly.Name == ServerAssembly)
                return true;

        return false;
    }

    public static ChunkNames? Resolve(Compilation compilation)
    {
        var chunk = Find(compilation, "ComponentChunk");
        var query = Find(compilation, "EntityQuery");

        if (chunk is null || query is null)
            return null;

        return new ChunkNames(
            chunk.ContainingNamespace.ToDisplayString(),
            query.ContainingNamespace.ToDisplayString());
    }

    private static INamedTypeSymbol? Find(Compilation compilation, string name)
    {
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            if (assembly.Name == ServerAssembly)
                return Search(assembly.GlobalNamespace, name);

        return null;
    }

    private static INamedTypeSymbol? Search(INamespaceSymbol containing, string name)
    {
        foreach (var type in containing.GetTypeMembers(name))
            return type;

        foreach (var child in containing.GetNamespaceMembers())
        {
            var found = Search(child, name);

            if (found is not null)
                return found;
        }

        return null;
    }
}
