# Native antiforgery on endpoint classes

This standalone example targets .NET 8 and .NET 10. Endpoint classes declare ASP.NET Core's exact native `RequireAntiforgeryTokenAttribute`. MediumAPI places class metadata before native inferred parameter/result metadata and original handler attributes. The host still owns antiforgery services, middleware, token issuance, cookies, identity and Data Protection.

The sample explicitly calls `AddAntiforgery` and `UseAntiforgery` after routing, with an ephemeral native Data Protection provider. Its token endpoint returns a request token and sets the corresponding native cookie. A caller sends both the cookie and request token, using the configured header or form field. Tokens and cookies remain sensitive values; the probes count handler calls without recording them.

## Routes

All routes use the `/endpoint-antiforgery` prefix.

| Route | Declaration and native behavior |
| --- | --- |
| `GET /token` | Issues a native request token/cookie pair. No antiforgery requirement is declared. |
| `POST /required` | Class declares true; native form inference also requires validation. Invalid or missing tokens return 400 before the form handler runs. |
| `POST /class-false` | Class declares false before native form inference subsequently declares true. This class attribute does not opt out of form validation. |
| `POST /method-disabled` | Class declares true; the original handler declares false after form inference, allowing the form without a token. |
| `POST /configured-disabled` | Class and handler declare true; `Configure` applies native `.DisableAntiforgery()` in a final convention. |
| `POST /group-disabled/required` | Class and handler declare true; a namespace group's native final opt-out remains effective. |
| `POST /json-required` | Class declares true. Middleware records validation success/failure; native JSON binding still invokes the handler, which returns the synthetic validation-feature flag. |
| `POST /json-class-false` | Class declares false, and JSON binding adds no inferred form requirement. Middleware skips validation and the synthetic flag is null. |

Form submissions use the `message` field. JSON submissions use the synthetic `JsonSubmission` contract. `FormProbe` records only dispatch counts. The JSON feature flag intentionally makes middleware behavior visible in this demonstration; it is not a validation or rejection policy.

## Native ordering and enforcement

A false class declaration is different from native `.DisableAntiforgery()`. Class metadata runs before native form inference; original method attributes run afterward. Native endpoint/group final conventions run later still. The library preserves those stages instead of moving false metadata to make it act as an opt-out.

Native antiforgery middleware records `IAntiforgeryValidationFeature` and calls the next delegate even after a validation failure. Native form binding consumes that feature and returns 400. An arbitrary JSON/custom handler does not gain automatic rejection from this attribute. Application-specific enforcement must follow the host's intended native validation approach. The companion tests exercise both invalid and valid JSON tokens and show the handler executing in both cases.

Effective true metadata still requires properly ordered native middleware, including for a non-form or GET endpoint. Missing services or middleware retain native configuration failures. The library does not install middleware, create a token service, add response declarations or introduce a CSRF engine. Native response contracts and host setup remain explicit.

Class lookup is declared-only on the concrete mapped endpoint. Derived antiforgery attributes and arbitrary custom metadata interfaces do not opt into this exact native rule. An inherited handler retains its original method attributes. Aliases, conditional groups, selection and per-builder object ownership retain their existing contracts.

## Verification

The companion integration project compares eight production routes against independent native mappings. Empty native groups put class-equivalent metadata before form inference without reusing MediumAPI's class reader. Tests inspect complete route/API Explorer snapshots, ordered metadata and actual HTTP behavior, including native cookie/header/form transport, invalid and mismatched tokens, cross-host ephemeral keys, method/final opt-outs and JSON validation-feature boundaries.

Scope checks cover discovery, aliases, inherited handlers, exact native type selection, fresh metadata, endpoint activation and native service/middleware configuration errors. Tokens use native issuance and validation; there is no replacement engine, test clock or manufactured validation result.

See the [endpoint antiforgery guide](../../../../Docs/endpoint-antiforgery.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed checks and compatibility boundaries.
