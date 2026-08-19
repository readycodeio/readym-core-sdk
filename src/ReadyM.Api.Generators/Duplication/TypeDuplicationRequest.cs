using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Duplication;

/// <summary>
/// Everything <see cref="TypeDuplicator"/> needs to copy one struct's members into a differently named struct.
///
/// The target is described by name, not by symbol: it does not have to exist. If the compilation happens to
/// declare a partial half under that name, the engine finds it and defers to it. If it does not, the whole type
/// is brought into existence.
/// </summary>
internal sealed class TypeDuplicationRequest(
    Compilation compilation,
    INamedTypeSymbol source,
    string targetName)
{
    /// <summary>The compilation the source lives in. Needed for semantic models over the source's syntax.</summary>
    public Compilation Compilation { get; } = compilation;

    /// <summary>The struct whose members are copied. Must be declared in <see cref="Compilation"/>.</summary>
    public INamedTypeSymbol Source { get; } = source;

    /// <summary>The name of the struct to produce.</summary>
    public string TargetName { get; } = targetName;

    /// <summary>Namespace to produce the target in. Null means the source's namespace.</summary>
    public string? TargetNamespace { get; set; }

    /// <summary>The namespace actually used: <see cref="TargetNamespace"/>, or the source's when that is null.</summary>
    public string? ResolvedNamespace => TargetNamespace ?? (Source.ContainingNamespace.IsGlobalNamespace
        ? null
        : Source.ContainingNamespace.ToDisplayString());

    /// <summary>Namespace-qualified name of the produced type. Use it to build a hint name.</summary>
    public string TargetFullName => ResolvedNamespace is null ? TargetName : ResolvedNamespace + "." + TargetName;

    /// <summary>Accessibility of the produced type. Null means the source's accessibility.</summary>
    public Accessibility? TargetAccessibility { get; set; }

    /// <summary>
    /// Emit the target as <c>partial</c>, so it can be extended by a hand-written half or by another generator.
    /// Forced on when a partial half already exists. Default true.
    /// </summary>
    public bool Partial { get; set; } = true;

    /// <summary>Member names never copied.</summary>
    public IReadOnlyCollection<string> ExcludedMemberNames { get; set; } = [];

    /// <summary>Copy attributes sitting on the copied members. Off strips every attribute list.</summary>
    public bool CopyAttributes { get; set; } = true;

    /// <summary>
    /// Copy the attributes on the source type declaration itself onto the produced type. Off by default: a
    /// duplicate usually wants its caller's attributes, not the original's, and copying them can re-trigger
    /// whichever generator the original was marked for.
    /// </summary>
    public bool CopyTypeAttributes { get; set; }

    /// <summary>Copy XML documentation comments attached to the copied members.</summary>
    public bool CopyDocumentation { get; set; } = true;

    /// <summary>
    /// Give the target the source's interfaces, remapped to itself, so <c>IEquatable&lt;Source&gt;</c> becomes
    /// <c>IEquatable&lt;Target&gt;</c>.
    /// </summary>
    public bool CopyInterfaces { get; set; } = true;

    public string InsertBlock { get; set; } = string.Empty;
}
