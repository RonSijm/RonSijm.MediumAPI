# Native antiforgery with namespace groups

This standalone example targets .NET 8 and .NET 10. MediumAPI forwards the native `Microsoft.AspNetCore.Antiforgery.RequireAntiforgeryTokenAttribute` declared directly on a namespace group definition to its route group. ASP.NET Core owns token creation, cookie handling, middleware, form binding and validation. MediumAPI adds no token implementation or enforcement filter.

The host explicitly registers `AddAntiforgery` and calls `UseAntiforgery` after `UseRouting`. In a host with authentication and authorization, place antiforgery middleware after both. This sample uses synthetic messages, has no authentication or database, and keeps its Data Protection keys in memory. Restarting it invalidates its existing tokens; production hosts must configure their own Data Protection key lifetime and storage.

## Request flow

1. Send `GET /antiforgery/token`. The response contains `requestToken`, `headerName` and `formFieldName`. Native `IAntiforgery.GetAndStoreTokens` also sets the antiforgery cookie. Preserve that cookie in the client; the request token alone is insufficient.
2. Send `POST /antiforgery/forms/required` as `application/x-www-form-urlencoded`, with a synthetic field such as `message=hello`. Send the cookie and put the request token in the returned header name (`X-Sample-Antiforgery`), or include it in the returned form field name (`__RequestVerificationToken`).
3. A valid cookie/token pair produces a JSON `FormReceipt` with the submitted message and handler identifier. Missing or invalid token validation produces the native HTTP 400 response before the handler runs.

Tokens and cookie values are never stored in the sample probe or logged by sample code. `FormProbe` records only fixed handler identifiers and invocation counts so the companion tests can verify that native form binding stops rejected requests.

## Native metadata precedence

All POST handlers bind `[FromForm] string message`. ASP.NET Core infers required antiforgery metadata for form binding. Group metadata is applied before that inference; handler attributes are applied afterward. `DisableAntiforgery()` uses native final conventions and takes precedence over those ordinary declarations.

| POST path | Declaration | Effective validation |
| --- | --- | --- |
| `/antiforgery/forms/required` | Required namespace group | Required |
| `/antiforgery/forms/endpoint-disabled` | Endpoint `Configure` calls `DisableAntiforgery()` | Disabled |
| `/antiforgery/forms/declared-false/inferred` | Child namespace declares `[RequireAntiforgeryToken(false)]` | Required: later native form inference wins |
| `/antiforgery/forms/declared-false/method-disabled` | Handler declares `[RequireAntiforgeryToken(false)]` | Disabled: handler metadata follows form inference |
| `/antiforgery/forms/declared-false/method-required` | Handler declares `[RequireAntiforgeryToken]` | Required |
| `/antiforgery/forms/fluent-disabled/method-required` | Group calls `DisableAntiforgery()`; handler declares required | Disabled: native final group convention wins |

The disabled routes exist only to demonstrate native opt-outs and precedence. Choose an opt-out deliberately based on the host's security requirements. A false namespace attribute is not a substitute for `DisableAntiforgery()` on a form group.

Antiforgery middleware records validation state; native form binding consumes that state and rejects invalid requests. A required metadata marker on an arbitrary JSON or non-form handler does not by itself guarantee rejection before the handler runs. Keep the native enforcement appropriate to the endpoint's binding model. Likewise, MediumAPI does not register host services, insert middleware, translate missing configuration failures, or change native metadata ordering.

The companion integration project compares these outcomes with equivalent native route groups and exercises valid, missing, malformed and mismatched cookie/token pairs on both target frameworks.

## Opt-in native annotation for a manually read form

The default seven-route form-binding scenario above remains available. Select
the separate three-route manual-form scenario with:

```text
dotnet run --project Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryExample --framework net10.0 -- --Sample:ManualForms=true
```

Fetch a cookie/token pair from `/antiforgery/token`, then post a synthetic
`message` field to `/manual-forms/upload` or its unnamed standalone
`/manual-forms-alias`. The original handler method declares the native
`[RequireAntiforgeryToken]`, whose default requirement is true. An
application-specific attribute that only implemented
`IAntiforgeryMetadata.RequiresValidation = true` can be replaced at that same
method position without introducing another MediumAPI attribute.

The handler deliberately retains `HttpContext`, its content-type check and
manual `ReadFormAsync` call. The application-owned `ManualFormGuard` still calls
the native antiforgery service before the handler. The native metadata marker
and middleware do not replace that guard. No automatic form binding, token
implementation or new validation pipeline is introduced.

The handler-owned no-store response header is installed after the guard. An
invalid header token produces HTTP 400 without that header or handler entry.
For a form missing its token, native middleware records an invalid form feature;
the guard's subsequent native revalidation can throw `InvalidOperationException`
when it tries to read that form. The old marker, native marker and MediumAPI
mapping preserve the same failure type/message before handler entry or its manual
form read. This example keeps that native failure boundary rather than adding a
catch or substituting a synthetic HTTP 400 response.

A valid token allows the handler's existing non-form HTTP 400 rejection to keep
the header, while avoiding a manual form read. Native antiforgery validation can
read form fields earlier. `ManualFormProbe` records only handler entry/read
counts and never token, cookie or submitted message values.

Seven added companion cases compare independent old-marker and native-method
references with the MediumAPI mapping, including metadata position, route and
API Explorer contracts, aliases, status/media/body bytes, native revalidation
exception parity, real cookie/header/form tokens and both public scenario selections. The previous 43 cases
remain unchanged, giving 50 cases per framework. See
[native manual-form annotations](../../../../Docs/namespace-tag-defaults.md#use-native-metadata-where-an-application-marker-adds-no-policy)
and the verification record for contracts and executed results.
