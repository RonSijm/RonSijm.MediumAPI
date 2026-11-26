# Native request timeouts on groups and endpoint classes

This standalone example targets .NET 8 and .NET 10. Namespace definitions and endpoint classes declare ASP.NET Core's exact native `RequestTimeoutAttribute` and `DisableRequestTimeoutAttribute`. The host explicitly calls `AddRequestTimeouts` and invokes `UseRequestTimeouts` after routing. ASP.NET Core owns timers, linked cancellation tokens, policy selection and timeout responses.

The normal sample policies use 30-second timeouts. Healthy requests return a synthetic value and whether the native timeout feature was present. For demonstration, add `?wait=true` to wait indefinitely with `RequestAborted` when a native timeout feature exists. This is controlled sample behavior, not an application work scheduler. Disabled or unconfigured requests complete normally. `TimeoutProbe` counts dispatch, observed cancellation and configured timeout responses without making timeout decisions.

## Routes

| Route | Declaration and native behavior |
| --- | --- |
| `GET /timeouts/inherited` | Inherits the namespace's named group policy. |
| `GET /timeouts/class` | Class selects the named class policy after the namespace attribute. |
| `GET /timeouts/method` | Original handler selects the named method policy after the class attribute. |
| `GET /timeouts/configured` | `Configure` supplies a native policy object, which takes priority over the attributes. |
| `GET /timeouts/duration` | Class declares a direct 30,000-millisecond duration. |
| `GET /timeouts/class-disabled` | Class disables timeouts; a later method enable declaration does not undo the marker. |
| `GET /timeouts/method-disabled` | Handler disables timeouts despite the class declaration. |
| `GET /timeouts/group-disabled/class-enabled` | Namespace disables timeouts despite the child class's named enable declaration. |
| `GET /open` | No endpoint timeout metadata. A host-configured default policy can still apply. |

## Selection and cancellation

Native timeout policy selection is not one uniform metadata-order rule:

- Any effective `DisableRequestTimeoutAttribute` bypasses the host default and endpoint timeouts, even if later metadata declares a timeout.
- The last native `RequestTimeoutPolicy` object takes priority over timeout attributes, regardless of their relative positions. Native fluent `WithRequestTimeout(TimeSpan)` and `WithRequestTimeout(RequestTimeoutPolicy)` add policy objects.
- If no policy object is present, the last `RequestTimeoutAttribute` selects its direct duration or named policy. Native fluent `WithRequestTimeout(string)` adds that attribute shape.
- If neither applies, the host default policy is used. The native default policy is unset unless the host configures it. Null or infinite effective timeout values retain their native behavior.

A timeout signals cancellation through the linked `RequestAborted` token. It does not terminate the handler or forcibly abort application work. The native middleware produces its default 504 or configured response when an uncaught timeout-related `OperationCanceledException` reaches it, the original client token is not canceled and the response has not started. Client aborts, started responses and other failures retain their normal native behavior.

Handling cancellation can allow a successful response. The companion fixture uses `NoContent` to demonstrate this without assuming body serialization ignores a canceled request token. Native `IHttpRequestTimeoutFeature.DisableTimeout()` can stop the pending timer; it does not cancel the request token. The host remains responsible for its cancellation and work-completion semantics.

The sample chooses synthetic custom statuses and a fixed policy marker header in native response callbacks. Those are sample host choices, not new MediumAPI defaults or response declarations. The handler's typed success metadata remains separate from middleware-generated timeout responses.

## Configuration and discovery

Metadata without `UseRequestTimeouts` is inert; it does not add a timeout guard. The middleware still requires `AddRequestTimeouts`, and missing registrations retain the native pipeline construction error. Unknown named policies retain native errors unless another native selection path or a disable marker preempts them. ASP.NET Core bypasses request timeouts while a debugger is attached.

Definition metadata follows `Map` and precedes the typed mount callback. Endpoint-class metadata precedes native inference/providers and original method attributes. Normal endpoint/group final conventions keep their native ordering. Class lookup remains declared-only; inherited original methods retain their own metadata. Exact native types opt in, without general custom-attribute discovery.

MediumAPI adds no timer, clock abstraction, cancellation engine, timeout response writer, policy registry or automatic middleware. Hosts choose services, policy values and middleware order. Aliases, conditional groups, original handler metadata, typed results, binding and endpoint ownership retain their existing contracts.

## Verification

The companion project maps the nine routes independently through native APIs, compares real metadata and complete route/API Explorer snapshots, and exercises accepted and timed-out requests. Most checks use healthy requests and generous native policy durations. A small set waits for actual native timer cancellation using 250-millisecond named policies or a 500-millisecond direct attribute, with generous client failure guards and no elapsed-time assertions. No test clock, replacement cancellation provider or custom timer is installed.

Scope checks cover both metadata hooks, inheritance, exact native type selection, isolated instances, discovery, aliases, missing configuration, unknown policies and native token/feature cleanup. Existing contract snapshots omit timeout-policy fields, so direct metadata and HTTP checks supply that evidence.

See the [request-timeout guide](../../../../Docs/request-timeouts.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed checks and compatibility boundaries.
