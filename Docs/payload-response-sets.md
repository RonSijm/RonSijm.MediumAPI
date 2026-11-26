# Response sets with payload and media contracts

[Documentation index](README.md)

A derived `EndpointResponsesAttribute` can now pass existing response objects to
the protected constructor taking `IReadOnlyList<EndpointResponseAttribute>`.
This reuses the existing inline expansion, while retaining each child's actual
CLR type, status, payload type and ordered content types.

For example, an application can name a repeated problem-response contract:

```csharp
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

internal sealed class ProblemResponseAttribute : EndpointResponseAttribute
{
    public ProblemResponseAttribute(int statusCode) : base(statusCode, typeof(ProblemDetails), "application/problem+json")
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
internal sealed class ProblemResponsesAttribute : EndpointResponsesAttribute
{
    public ProblemResponsesAttribute(params int[] statusCodes) : base(CreateResponses(statusCodes))
    {
    }

    private static IReadOnlyList<EndpointResponseAttribute> CreateResponses(int[] statusCodes)
    {
        ArgumentNullException.ThrowIfNull(statusCodes);
        return Array.ConvertAll(statusCodes, status => (EndpointResponseAttribute)new ProblemResponseAttribute(status));
    }
}
```

Then `[ProblemResponses(400, 404, 409)]` can replace those three adjacent
`[ProblemResponse]` declarations at their original slot. The new set supplies
actual `ProblemResponseAttribute` children. Converting them to ordinary base
responses would discard their CLR identity, even if the visible fields matched.
This constructor also accepts mixed response subclasses and media contracts;
they need not share a payload or content type.

## Membership is copied; response objects are retained

The protected constructor snapshots the supplied list into a read-only collection.
Later list mutations do not change membership or ordering. Duplicates, including
repeated references to the same child, remain in their original positions.
A null list throws `ArgumentNullException`; a null child throws `ArgumentException`,
both naming `responses`. An empty list is valid.

The constructor retains each supplied child object; it does not deep-clone an
arbitrary subclass. Ordinary response state/content-type collections keep their
existing immutable behavior. An application's additional mutable subclass state
remains application-owned. Construct fresh children for fresh declarations when
that is the intended ownership; the library adds no global cache or reuse policy.
Nested response sets are not accepted as children: their marker type is separate
from `EndpointResponseAttribute`.

The public `params int[]` constructor keeps its existing status-only behavior and
signature. The new constructor is protected and non-params, so `[EndpointResponses]`
and existing derived `base()` calls continue to select the status constructor.
In a derived constructor, `base(null)` is now ambiguous between the two overloads;
use an explicitly typed list/array when testing or intentionally passing null.
A normal external `new EndpointResponsesAttribute(null)` still selects the public
status-array constructor. No new public attribute-argument syntax is introduced.

## The same expansion and native priorities apply

Supported endpoint classes, original methods, namespace groups and registered
filter/factory classes use the existing response-set phases. The marker is
removed and its children are inserted at that position. Extra interfaces on the
authoring container are not forwarded; each child retains its original metadata
identity and interfaces. Keep children limited to the intended response contract.

Native typed-result inference, MVC provider behavior, ordinary `Add`/`Finally`,
response fallbacks, aliases, inherited method metadata and fresh mounts keep
their existing rules. Direct class declarations remain declared-only. Bare native
mapping does not opt into set expansion. Arbitrary native metadata injection has
the same conditional-callback limitations documented for [response sets](response-sets.md).

Response metadata documents possible outcomes. It does not serialize or validate
results, implement exception handling, or apply authentication/authorization.
Keep explicit handler outcomes as typed results where practical; exceptions for
routine control flow are generally an anti-pattern.

## Migrating a repeated contract

Replace only an exact adjacent run at the same owner and declaration position.
Retain the old child CLR types and every status/type/media value and ordering.
Keep other responses, handlers, filters, headers, groups, names and paths unchanged.
Review code that reflects on raw handler attributes: MethodInfo remains original,
but its authored attributes now contain a set rather than separate children.

The shared-response example compares exact child identities, full ordered raw
contracts, API Explorer, complete OpenAPI and HTTP against independent before/native
references. See [derived responses](derived-responses.md), [shared response declarations](shared-response-declarations.md)
and the [verification record](testing-and-development.md#current-verification-record).
