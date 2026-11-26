# Native endpoint-class CORS

This standalone example targets .NET 8 and .NET 10. MediumAPI forwards exact native `EnableCorsAttribute` and `DisableCorsAttribute` declarations directly on endpoint classes. ASP.NET Core owns policy selection, response headers, preflight handling and browser-facing CORS behavior. MediumAPI adds no CORS middleware or policy evaluator.

Run from the repository root, for example:

```shell
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointCorsExample/RonSijm.MediumAPI.EndpointCorsExample.csproj --framework net8.0 --urls http://localhost:5080
```

The host explicitly registers `AddCors`, calls `UseRouting`, then calls `UseCors`. It registers a default policy and two named policies using synthetic `.example` origins. There is no authentication, database or application identity service in this example. CORS is not an authentication or authorization control.

## Routes and native precedence

All eight routes are GET handlers returning a typed JSON `CorsReceipt`. Their native HTTP methods and default endpoint names remain unchanged; no separate OPTIONS endpoint is registered.

| Path | Declarations | Effective native CORS metadata |
| --- | --- | --- |
| `/cors/open` | No endpoint CORS declaration | Middleware/default policy selection |
| `/cors/default` | Class `[EnableCors]` | Bare enable; host middleware/default policy selection |
| `/cors/named` | Class `[EnableCors(PolicyName = SamplePolicies.Browser)]` | Named Browser policy; named-property assignment is preserved |
| `/cors/enabled-group/class-disabled` | Enabled parent namespace, class `[DisableCors]` | Disabled |
| `/cors/disabled-group/class-enabled` | Disabled parent namespace, class named enable | Named Browser policy |
| `/cors/class-disabled-method-enabled` | Class disable, handler named enable | Named Browser policy |
| `/cors/class-enabled-method-disabled` | Class named enable, handler disable | Disabled |
| `/cors/configured` | Class Browser, endpoint `Configure` calls native `RequireCors(Override)` | Named Override policy |

The native middleware selects the last effective `ICorsMetadata`. Group metadata precedes class metadata, inferred/provider metadata, handler attributes and endpoint conventions. A later method or fluent enable can therefore replace an earlier disable. No CORS policy-property merging is added. This is different from the broad disabling semantics of native rate limiting.

Bare `[EnableCors]` carries a null policy name. With the host's ordinary `UseCors()`, the native default policy is selected. If the host instead selects `UseCors("Override")`, bare enable retains that middleware selection; it does not force the default policy. An explicit named class declaration can select its own policy. Missing named policies retain native no-policy behavior rather than becoming authorization-style policy errors.

## Requests and browser behavior

For an ordinary request, send an Origin header such as:

```shell
curl -i -H "Origin: https://client.example" http://localhost:5080/cors/named
```

An accepted origin receives the applicable native CORS response headers. A disallowed origin still receives the ordinary HTTP 200 JSON response from the handler, but without `Access-Control-Allow-Origin`; a browser enforces the cross-origin access restriction. Requests without `Origin` keep normal handler behavior without CORS response headers. Disabling CORS does not block a direct HTTP client from calling an endpoint.

For preflight:

```shell
curl -i -X OPTIONS -H "Origin: https://client.example" -H "Access-Control-Request-Method: GET" -H "Access-Control-Request-Headers: X-Client" http://localhost:5080/cors/named
```

With a resolved policy, native routing recognizes preflight for CORS endpoint metadata and the middleware returns HTTP 204 before handler execution. A missing named policy can instead fall through to the handler, including for preflight, with no CORS allow headers. Native disabled or rejected-origin preflight responses also omit browser allow headers. The Browser policy declares two allowed origins, credentials, GET/POST methods, `X-Client`, and a 600-second preflight max age. It advertises `X-Sample-Value` as an exposed response-header name; CORS policy configuration does not itself create that application header.

The native response advertises configured methods and headers. The browser compares its request to those lists; a server HTTP 204 alone does not prove that a browser will allow the request. The tests deliberately retain that distinction for an unsupported requested header.

## Verification sources

The companion integration project compares independent native route groups and handlers with the class-based registrations. Its primary checks cover complete route/API Explorer inventories, ordered CORS metadata, plain requests, origins, preflight side effects, credentials and policy headers, native overrides, links, original handler signatures and independent host policies. Additional scope fixtures cover discovery, inheritance, exact attribute types, aliases and explicit host configuration boundaries.

`CorsProbe` records only fixed handler names and invocation counts. Tests use synthetic origins and services, and start ordinary ASP.NET Core TestServer hosts. They do not substitute a custom CORS implementation or a browser simulation for native middleware.

See the [endpoint CORS guide](../../../../Docs/endpoint-cors.md) for the supported class-attribute scope and the [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed results and remaining compatibility limits.
