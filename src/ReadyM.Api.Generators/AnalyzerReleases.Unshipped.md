; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
READYM001 | ReadyM | Warning | The server SDK is referenced but its chunk types were not found, so queries fall back to walking identities.
READYM002 | ReadyM | Error | Growing the world inside a query loop, which the SDK refuses at run time.
READYM004 | ReadyM | Error | A type named by [ExplicitComponent] cannot back a shape.
READYM005 | ReadyM | Error | An explicit component has no member matching a declared accessor.
READYM006 | ReadyM | Error | A setter was declared over a member that cannot be assigned.
READYM007 | ReadyM | Error | An explicit component is not visible from the declaring assembly.
