using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    public const string ChunkAssembly = "ReadyM.SDK.Chunks";

    private ChunkNames(string chunks, ImmutableArray<QueryTarget> queries)
    {
        EntityQueries = queries;
        ComponentChunk = $"global::{chunks}.ComponentChunk";
        ChunkSlot = $"global::{chunks}.ChunkSlot";
        ChunkView = $"global::{chunks}.IArchetypeChunkView";
        ChunkQuery = $"global::{chunks}.ChunkQuery";
        ChunkViews = $"global::{chunks}.ChunkViews";
    }

    public string ComponentChunk { get; }

    public string ChunkSlot { get; }

    public string ChunkView { get; }

    public string ChunkQuery { get; }

    public string ChunkViews { get; }

    /// <summary>
    /// One per half the compilation can see. A mod references one, but a project that tests both
    /// sees two, and a shape is queryable through either.
    /// </summary>
    public ImmutableArray<QueryTarget> EntityQueries { get; }

    /// <summary>
    /// One half's query type, and how many shapes it can be asked for.
    /// </summary>
    /// <remarks>
    /// The halves do not go equally far: the client stops at two, since a netstandard2.0 build
    /// cannot carry the composed views, and the server goes to six. Emitting a binding for an arity
    /// a half does not have would not compile.
    /// </remarks>
    internal sealed class QueryTarget(string qualified, ImmutableArray<int> arities)
    {
        public string Qualified { get; } = qualified;

        public bool Supports(int arity) => arities.Contains(arity);
    }

    /// Whether a compilation could use the fast path at all.
    public static bool ChunkAssemblyReferenced(Compilation compilation)
    {
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            if (assembly.Name == ChunkAssembly)
                return true;

        return false;
    }

    public static ChunkNames? Resolve(Compilation compilation)
    {
        var chunk = Find(compilation, "ComponentChunk");
        var queries = FindAll(compilation, "EntityQuery");

        if (chunk is null || queries.Length == 0)
            return null;

        return new ChunkNames(chunk.ContainingNamespace.ToDisplayString(), queries);
    }

    /// <summary>
    /// Searches every referenced assembly, not just the shared one: the chunk types live there, but
    /// the query type a binding hangs off belongs to whichever half the mod is compiled against.
    /// </summary>
    /// <summary>Every namespace declaring a type of this name, across the halves in scope.</summary>
    private static ImmutableArray<QueryTarget> FindAll(Compilation compilation, string name)
    {
        var found = ImmutableArray.CreateBuilder<QueryTarget>();

        foreach (var assembly in Assemblies(compilation))
        {
            var type = Search(assembly.GlobalNamespace, name);

            if (type is null)
                continue;

            var arities = type.ContainingNamespace
                .GetTypeMembers(name)
                .Select(candidate => candidate.Arity)
                .ToImmutableArray();

            found.Add(new QueryTarget($"global::{type.ContainingNamespace.ToDisplayString()}.{name}", arities));
        }

        return found.ToImmutable();
    }

    private static IEnumerable<IAssemblySymbol> Assemblies(Compilation compilation)
        => new[] { compilation.Assembly }
            .Concat(compilation.SourceModule.ReferencedAssemblySymbols)
            .Where(assembly => assembly.Name.StartsWith("ReadyM.SDK", System.StringComparison.Ordinal));

    private static INamedTypeSymbol? Find(Compilation compilation, string name)
    {
        foreach (var assembly in Assemblies(compilation))
        {
            var found = Search(assembly.GlobalNamespace, name);

            if (found is not null)
                return found;
        }

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
