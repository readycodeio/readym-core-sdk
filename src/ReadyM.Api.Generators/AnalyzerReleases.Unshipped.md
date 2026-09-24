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
READYM008 | ReadyM | Error | An [ExplicitCollection] name matches no member of the component.
READYM017 | ReadyM | Error | A client writes a replicated value straight through its setter.
READYM019 | ReadyM | Error | A client changes a replicated collection through its own members.
READYM022 | ReadyM | Error | A system is not declared partial.
READYM023 | ReadyM | Error | A system has no update, so nothing would ever run it.
READYM024 | ReadyM | Error | A system has more than one update the SDK could call.
READYM025 | ReadyM | Error | A method named Update cannot be called as one.
