namespace ReadyM.Api.Generators.Duplication;

/// <summary>
/// What went wrong while duplicating a type. Deliberately free of Roslyn's <c>DiagnosticDescriptor</c> so the
/// duplication engine stays independent of any single generator's diagnostic ids.
/// </summary>
internal enum TypeDuplicationIssueCode
{
    /// <summary>The type named as the duplication source is not a struct.</summary>
    SourceNotStruct,

    /// <summary>The source type has no syntax in this compilation, so its member bodies cannot be copied.</summary>
    SourceNotInCompilation,

    /// <summary>The target type, or one of the types it is nested in, is not declared <c>partial</c>.</summary>
    TargetNotPartial,

    /// <summary>Source and target differ in generic arity, so member signatures cannot be mapped across.</summary>
    GenericArityMismatch,

    /// <summary>Source and target are the same type.</summary>
    SourceIsTarget
}

/// <summary>A single problem found while duplicating a type.</summary>
internal sealed class TypeDuplicationIssue(TypeDuplicationIssueCode code, string message)
{
    public TypeDuplicationIssueCode Code { get; } = code;

    public string Message { get; } = message;
}
