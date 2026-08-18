using System.Collections.Generic;

namespace ReadyM.Api.Generators.Duplication;

/// <summary>Outcome of a <see cref="TypeDuplicator.Duplicate"/> call.</summary>
internal sealed class TypeDuplicationResult(
    string? source,
    IReadOnlyList<TypeDuplicationIssue> issues,
    int copiedMemberCount)
{
    /// <summary>The generated C# file, or <c>null</c> when duplication failed.</summary>
    public string? Source { get; } = source;

    /// <summary>Problems found. A non-empty list with a <c>null</c> <see cref="Source"/> means a hard failure.</summary>
    public IReadOnlyList<TypeDuplicationIssue> Issues { get; } = issues;

    /// <summary>How many member declarations were actually copied.</summary>
    public int CopiedMemberCount { get; } = copiedMemberCount;

    public static TypeDuplicationResult Failed(TypeDuplicationIssueCode code, string message)
        => new(null, [new TypeDuplicationIssue(code, message)], 0);
}
