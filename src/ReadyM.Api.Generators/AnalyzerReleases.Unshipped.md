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
READYM022 | ReadyM | Error | A service is not declared partial.
READYM023 | ReadyM | Error | A service is not declared sealed.
READYM024 | ReadyM | Error | A method named Update, Start or Stop cannot be called as one.
READYM025 | ReadyM | Error | A handler declared in a service cannot be called.
READYM026 | ReadyM | Error | A handler declared in a service does not name the shape it watches.
READYM028 | ReadyM | Error | A delete handler a shape declared for itself cannot be called.
READYM029 | ReadyM | Error | A shape declares more than one delete handler.
