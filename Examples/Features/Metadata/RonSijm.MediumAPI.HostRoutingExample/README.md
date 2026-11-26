# Native host routing

This standalone example targets .NET 8 and .NET 10. Namespace definitions and endpoint classes declare ASP.NET Core's exact native `HostAttribute`. MediumAPI forwards namespace declarations after the group's `Map` and endpoint-class declarations before native inference and original handler attributes. ASP.NET Core's routing matcher makes the actual host-selection decision.

The sample uses ordinary minimal hosting and scoped endpoint discovery. It adds no host-filtering middleware, DNS/proxy policy, authentication or replacement matcher. `HostProbe` records synthetic handler-dispatch counts; it does not make routing decisions.

## Routes

| Route | Effective native host metadata |
| --- | --- |
| `GET /hosts/inherited` | Namespace declaration `group.example.test`. |
| `GET /hosts/class` | Class alternatives `api.example.test` or `alternate.example.test`, replacing the namespace declaration. |
| `GET /hosts/method` | Original handler selects `method.example.test` after the class declaration. |
| `GET /hosts/configured` | `Configure` selects `configured.example.test` after namespace, class and method declarations. |
| `GET /hosts/wildcard` | Class declares `*.example.test`, including nested subdomains and excluding the bare `example.test` apex. |
| `GET /hosts/port` | Class alternatives `port.example.test:8443`, `secure.example.test:443` or `web.example.test:80`. |
| `GET /hosts/empty` | Class explicitly declares an empty host list, replacing the parent restriction and accepting any host. |
| `GET /hosts/child/value` | Child namespace selects `child.example.test`, replacing the parent namespace declaration. |
| `GET /open` | No host metadata; accepts any host. |

Host patterns are application-owned constants in `SampleHosts`, using reserved synthetic names. A host mismatch returns the native routing result, normally 404 in this sample, without invoking its endpoint. The route paths, names, typed payloads and ordinary binding remain independent from host matching.

## Native selection rules

The last effective `IHostMetadata` value replaces earlier declarations. Host lists from ancestors, classes, methods and fluent conventions are not intersected or merged. Within the selected list, host entries are alternatives. Consequently a child/class override can broaden exposure; an empty list clears an inherited restriction. Keep these native semantics when reorganizing registrations.

Host names compare without case sensitivity. An omitted pattern port accepts any request port; an explicit pattern port restricts it. When a request host has no explicit port, the native matcher uses the scheme's default, 80 for HTTP and 443 for HTTPS. A suffix wildcard matches subdomains rather than the bare apex. The example tests these supported cases without replacing native parsing or claiming complete hostname grammar coverage.

Host routing uses `HttpRequest.Host`. It is separate from authorization, host-header validation, TLS termination and trusted forwarded-header configuration. Those remain host/deployment responsibilities. This attribute does not make a client-supplied host header trusted or provide a tenant authorization decision.

## Verification

The companion project compares nine production routes with independently composed native routes. Native empty groups place class-equivalent metadata before inference and method attributes. Checks compare ordered/effective `IHostMetadata`, route/API Explorer snapshots and actual HTTP responses. Existing snapshots omit host patterns, so snapshot equivalence alone does not prove host-selection behavior.

The TestServer helper sends an explicit HTTP Host header and controls the request URI scheme without DNS or TLS connections. A null helper argument keeps the transport URI's host; it does not simulate a literally absent HTTP Host header. Tests use synthetic probes to verify wrong-host requests never reach handlers.

Scope checks cover exact native metadata, fresh objects/collections, discovery, aliases, inherited handlers, final conventions and native parsing boundaries. The runtime adds metadata only: no new host service, matcher, filter, schema field or automatic request policy is introduced.

See the [host-routing guide](../../../../Docs/host-routing.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed checks and compatibility limits.
