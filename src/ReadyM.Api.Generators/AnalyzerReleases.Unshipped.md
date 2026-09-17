; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
READYM001 | ReadyM | Warning | The server SDK is referenced but its chunk types were not found, so queries fall back to walking identities.
READYM002 | ReadyM | Error | Growing the world inside a query loop, which the SDK refuses at run time.
