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
ENDPOINT009 | EndpointRouting | Error | Duplicate namespace endpoint group definitions
ENDPOINT010 | EndpointRouting | Error | Unsupported HandleAsync declaration
ENDPOINT011 | EndpointRouting | Error | Invalid endpoint Configure signature
ENDPOINT012 | EndpointRouting | Error | Endpoint type cannot be referenced by generated registration
ENDPOINT013 | EndpointRouting | Error | Namespace group cannot be referenced by generated registration
ENDPOINT014 | EndpointRouting | Info | Explicit route binding absent from readable declared layout; outer host may provide it
ENDPOINT015 | EndpointRouting | Info | Inherited namespace route constraint type mismatch
ENDPOINT016 | EndpointRouting | Error | Invalid declarative endpoint pattern or generated Pattern property conflict
