# Sharing optional query inputs

[Documentation index](README.md)

Related read endpoints can share an ordinary native `[AsParameters]` transport
object. This removes repeated handler parameters while retaining ASP.NET Core
binding and the original handler metadata. Existing MediumAPI support is
sufficient; this pattern adds no runtime API, binder or validation mechanism.

```csharp
using Microsoft.AspNetCore.Mvc;

internal sealed class ReadQuery
{
    public ReadQuery()
    {
    }

    // Preserve existing CLR names as well as wire names during this migration.
    [FromQuery(Name = "search")]
    public string? search { get; set; }

    [FromQuery(Name = "page")]
    public int? page { get; set; }
}
```

Receive the object as `[AsParameters] ReadQuery query`, importing
`Microsoft.AspNetCore.Http` for the attribute. Keep services and cancellation
tokens as separate handler parameters. Use a public parameterless constructor
and public ordinary get/set properties. Preserve the old declaration order,
nullable types and explicit wire names; omit new initializers, default values,
validation and normalization from the transport class.

## Preserve CLR names when binding diagnostics are part of the contract

`FromQuery.Name` controls the query key. Native parse-failure diagnostics can use
the CLR property name through PropertyAsParameterInfo instead. A property named
`Page` with `Name = "page"` can therefore keep the wire key while changing an
exception message that previously named scalar parameter `page`.

The compatibility example deliberately retains the original camel-case CLR names.
It compares the complete native BadHttpRequestException type/message in throwing
mode, and ordinary HTTP responses in nonthrowing mode. Keeping the wire alias
alone is not sufficient evidence of diagnostic compatibility. Native source:
[PropertyAsParameterInfo on .NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Shared/PropertyAsParameterInfo.cs),
[PropertyAsParameterInfo on .NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Shared/PropertyAsParameterInfo.cs).

## Keep normalization and use-case boundaries

The runnable example shares twelve optional inputs between search and workspace
reads, in this exact order:

| CLR member and query key | Type |
| --- | --- |
| search | string? |
| customerId | long? |
| platform | bool? |
| action | string? |
| entityType | string? |
| userId | long? |
| from | string? |
| to | string? |
| page | int? |
| pageSize | int? |
| projectId | long? |
| kind | string? |

The original request-building helper remains responsible for translating these
values. Missing search becomes an empty string; platform uses `== true`; missing
page/pageSize become 1/10. Raw date strings are parsed with the same exact format;
an invalid date becomes null. Binding those fields as DateOnly instead would
reject inputs that the existing helper tolerates.

Absent defaults in the binding/documentation contract remain absent even when a
later helper supplies use-case defaults. Do not move paging defaults or range
clamping into property initializers. Zero, negative and boundary numeric values
keep native parsing behavior and proceed to the same downstream policies.
Likewise, preserve omitted, empty, whitespace and repeated values by comparing
with native scalar behavior instead of guessing a new normalization rule.

Explicit query sources do not gain a fallback from headers, request bodies or
route values. Keep all existing routes, constraints, names, response metadata,
headers, filters, authorization and Application calls in place. Do not bind a
domain/use-case record directly when its names, types or defaults differ from the
existing HTTP inputs.

## The CLR argument list changes

Twelve scalar query inputs plus service/token become one aggregate plus
service/token: fourteen native invocation arguments become three. MediumAPI
does not flatten the object back into old positions. Review argument-sensitive
filters and factories before changing a handler signature. A filter that only
awaits `next` and translates selected existing exceptions can remain unchanged.
Exceptions for routine control flow remain an anti-pattern; this preserves an existing translation.

Native binding failures must keep zero handler dispatch and service operation calls. Filter events follow
the selected native framework pipeline; do not assume every filter is skipped
on every nonthrowing parse failure. Retain native status, exception, ordering and
short-circuit behavior rather than adding another request-processing layer.

## Runnable verification

The existing [endpoint documentation example](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample/)
contains an opt-in shared-query scenario:

```text
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample --framework net10.0 -- --Sample:SharedQueries=true
```

Its paths are `/query-sample/audit-logs` and `/query-sample/workspace`. Independent
scalar-before, native-aggregate and MediumAPI hosts compare complete route and
API Explorer snapshots, strict whole-document generated OpenAPI, synthetic HTTP,
normalization and both throwing/nonthrowing binding failures. The Swagger provider
is already a dependency of this example; this increment adds no package.

Generated documents remain provider/version/host-specific. These examples do not
run a consuming application, its authorization or persistence, and they cannot
establish its complete deployed-host contract. See the example READMEs and
[verification record](testing-and-development.md#current-verification-record)
for the exact executed cases and targets.
