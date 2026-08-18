using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Duplication;

/// <summary>
/// Everything <see cref="TypeDuplicator"/> needs to copy one struct's members onto another partial struct.
/// </summary>
internal sealed class TypeDuplicationRequest(
    Compilation compilation,
    INamedTypeSymbol source,
    INamedTypeSymbol target)
{
    /// <summary>The compilation both types live in. Needed for semantic models over the source's syntax.</summary>
    public Compilation Compilation { get; } = compilation;

    /// <summary>The struct whose members are copied.</summary>
    public INamedTypeSymbol Source { get; } = source;

    /// <summary>The partial struct the copied members are emitted into.</summary>
    public INamedTypeSymbol Target { get; } = target;

    /// <summary>Member names never copied, whatever the target declares.</summary>
    public IReadOnlyCollection<string> ExcludedMemberNames { get; set; } = [];

    /// <summary>Copy attributes sitting on the copied members. Off strips every attribute list.</summary>
    public bool CopyAttributes { get; set; } = true;

    /// <summary>Copy XML documentation comments attached to the copied members.</summary>
    public bool CopyDocumentation { get; set; } = true;

    /// <summary>
    /// Add the source's interfaces to the generated partial, with references to the source type remapped to the
    /// target (so <c>IEquatable&lt;Source&gt;</c> becomes <c>IEquatable&lt;Target&gt;</c>).
    /// </summary>
    public bool CopyInterfaces { get; set; } = true;
}
