; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ENDPOINT001 | EndpointRouting | Error | Route parameter missing from handler
ENDPOINT002 | EndpointRouting | Error | Handler parameter missing from route
ENDPOINT003 | EndpointRouting | Error | Route constraint type mismatch
ENDPOINT004 | EndpointRouting | Warning | Endpoint lambda should be static
ENDPOINT005 | EndpointRouting | Warning | Pattern property missing StringSyntax attribute
ENDPOINT007 | EndpointRouting | Error | HandleAsync method missing
ENDPOINT008 | EndpointRouting | Warning | Endpoint metadata unparseable
