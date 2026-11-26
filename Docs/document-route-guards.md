# Conditional document route guards

[Documentation index](README.md)

The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
also demonstrates existing native filter factories on document entry routes.
This is an application adapter pattern; it adds no runtime API, router, frontend
server or validation engine to MediumAPI.

## Keep exposure and request checks separate

A namespace's ShouldMap controls whether its routes are exposed by a host.
A guard factory controls an already mapped request. Keep conditional mounting
with the host/group, and attach a factory only to document handlers that share
its complete parameter and response contract. A manual document alias also needs
its guard on the endpoint; it must not depend on automatic namespace discovery.

For example, a selected project-document handler can keep its existing result
union while moving a repeated positive-ID check to a metadata-free native factory:

```csharp
[EndpointFilterFactory<ProjectDocumentGuardFactory>]
public Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> HandleAsync(long customerId, long projectId, HttpContext context, IWebHostEnvironment environment, DocumentOptions options)
{
    return DocumentEntry.Open(context, environment, options);
}
```

The factory uses exact [named argument lookup](factory-arguments.md) during native
pipeline construction, then reads current long arguments in its returned delegate.
Retain the exact positive-ID predicate and comparison order. Invalid values return
the same outer Results union carrying NotFound; valid values call next. Preserve
the handler's public signature, declarations, names and complete return type.
DocumentEntry and DocumentOptions are illustrative application code.

Do not use a namespace-wide factory when sibling handlers have other parameters.
Do not replace a handler guard with route constraints without reviewing routing,
binding and middleware differences. A positive identifier is a syntactic check,
not an authorization or resource-existence decision. Services keep those decisions.

## Preserve exact key matching

If an existing route uses Regex.IsMatch with `^[a-zA-Z0-9_-]{1,80}$` and
RegexOptions.CultureInvariant, retain that literal, flags and short-circuit order.
For a customer/key route, an invalid customer must avoid key evaluation if the
original conjunction did. Null and exceptional direct invocation behavior should
keep its original boundary.

Do not replace the expression with trimming, character/length logic or a stricter
end anchor during a preservation migration. The native `$` behavior around a final
newline can differ from an assumed whole-input match. Compatibility examples test
that existing behavior rather than silently strengthening it.

## Leave entry, headers and aliases in place

An invalid guard must not invoke the entry adapter, inspect a file or gain entry
headers. Valid requests retain the same entry call, no-store assignment, redirect
decision, exact Location, physical-file bytes and media. Outer host middleware,
authentication, origin selection, static assets and cache rules remain with the
application. MediumAPI does not supply those policies.

Repeated explicit aliases retain their original patterns, ordering and native
fallback-name suppression. Add the guard to their endpoint class or original
method; do not add separate mapping loops or duplicate names. Keep the conditional
group and manual alias registrations unchanged. See [bulk aliases](bulk-aliases.md)
and [filter factories](filter-factories.md).

Preserve metadata independently from execution: a metadata-free factory adds no
response declaration, and the full handler union keeps native response inference.
Filter-returned early results can keep the same concrete nested representation
when outer filters rely on it. Keep header writes at their original boundary;
see [response headers](response-headers.md).

## Verification scope

The example uses synthetic options/content and independent native reference paths.
It checks conditional exposure, positive/missing route values, literal key behavior,
guard result/header boundaries, explicit aliases and native response metadata.
It does not validate another application's actual frontend package, host/origin
middleware, deployed redirects, tenancy, assets or complete generated OpenAPI.
Read the [current verification record](testing-and-development.md#current-verification-record)
for executed cases and target frameworks. Consumer verification is compilation
only; no consuming tests/application or real document requests run in this increment.
