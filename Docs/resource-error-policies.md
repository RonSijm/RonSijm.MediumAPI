# Resource-specific error policies

[Documentation index](README.md)

The same example now includes [operation outcome policies](operation-outcome-policies.md) for inner replay/conflict handling and exact whole-handler catch boundaries.

The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
uses existing native endpoint filters to organize three application policies:
repository commands, privacy-preserving conditional reads and report exports.
It adds no MediumAPI runtime API or replacement middleware. Each policy belongs
to the application; MediumAPI registers its filter at the selected endpoints.

## Match the complete policy

Two catch lists are not interchangeable merely because both catch a missing
resource or validation exception. An established repository command might map
forbidden access to 403, a missing item to 404, concurrency to 409 and a provider
resource failure to a safe 400. Its validation response might deliberately use
one fixed code instead of exposing the exception's custom code.

A privacy-sensitive read can intentionally map both forbidden and missing
resources to the same 404 problem. An export can have the same statuses but
different safe titles and codes. Use separate filters for those contracts.
Preserve catch order, exact payloads, cancellation and unknown-exception behavior.
Do not reuse a general command filter that broadens or changes those mappings.

```csharp
public sealed class ResourceReadFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (AccessDeniedException)
        {
            return ResourceProblems.Unavailable();
        }
        catch (ResourceMissingException)
        {
            return ResourceProblems.Unavailable();
        }
    }
}
```

These exceptions and problem helpers are application examples, not library APIs.
Register the filter only on handlers whose existing protected boundary matches:

```csharp
[EndpointFilter<ResourceReadFilter>]
public async Task<Results<Ok<ResourceDto>, ProblemHttpResult>> HandleAsync(ResourceService resources, CancellationToken cancellationToken)
{
    return TypedResults.Ok(await resources.ReadAsync(cancellationToken));
}
```

Application services retain authorization, tenant checks and data access. If an
ownership query was already inside a callback before a mutation, keep that order
and its arguments unchanged. Do not move that query into a second filter or put
the new policy on an entire namespace that contains different error contracts.

## Keep conditional response work in its original boundary

An ETag is not an authorization shortcut. Preserve the authorized read before
serialization, hashing, header assignment or evaluating If-None-Match. Preserve
the exact serialization bytes, hash representation, comparison rules and 304
result; adopting a filter is not a cache implementation redesign.

The example retains a specific legacy comparison of a strong tag, its weak form
and a wildcard. This is a compatibility example, not a complete HTTP caching
algorithm. Dynamic tags remain application-owned; a constant ResponseHeader
attribute cannot calculate a resource version or preserve arbitrary mid-handler
timing.

If a header was assigned before a later failure, keep it assigned at that point.
An unconditional declaration writes earlier, while a success-selected header
writes later. Either can change errors even when success responses look identical.
Existing request guards stay outside the new filter when they were outside the
original helper. Native binding and middleware keep their ASP.NET Core positions.

## Keep file and metadata contracts

Leave file construction in the handler/helper: the bytes, content type and
optional download name have observable effects. An HTML response with no download
name is different from a PDF response with an attachment filename. Preserve both
on the original FileContentHttpResult path.

A migrated helper can become an awaited callback bridge while keeping its full
declared Results union, including nested success alternatives. Native typed
metadata still describes that union even when filters return failures directly.
Keep explicit 304, file media and problem schema declarations that the return type
does not express. Retain an accepted response's exact Location value and format.

A plain filter without response attributes adds no response metadata. Use that
form when preserving existing declarations and deliberate omissions. Moving a
catch does not justify silently adding a missing 409 or replacing a typed 404
with a different problem schema. Registering a filter and calling a static policy
method have different metadata effects; see [input policies](input-error-policies.md).

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** These adapters preserve existing contracts. Prefer explicit application
outcomes and typed HTTP results for newly designed expected validation paths.
Do not return provider messages, resource contents or exception details to clients.

## Verification scope

The example compares selected HTTP and native metadata/API Explorer contracts
with independent legacy/native paths and fixed synthetic services/content.
It does not prove another application's tenant permissions, actual provider
operations, generated reports, persistence or complete generated OpenAPI document.
Read the [current verification record](testing-and-development.md#current-verification-record)
for executed cases and target frameworks. Consumer verification remains compilation
only; consuming application tests and endpoint execution are separate work.
