# Native antiforgery and manual-form checks

This companion project targets the example's existing .NET 8 and .NET 10
frameworks. Its previous 43 cases cover native form inference, namespace and
method declarations, final opt-outs, native cookie/header/form tokens, isolated
key lifetimes, host setup and metadata-only boundaries.

`ManualFormAnnotationTests` adds seven cases through a separate runnable
three-route scenario. Independent native references keep the old custom
`IAntiforgeryMetadata` marker and its native `RequireAntiforgeryTokenAttribute`
replacement on the original method. Full selected route and API Explorer
contracts match. The requirement stays true at the method metadata position;
the standalone alias keeps that declaration without importing group metadata.

Actual native cookie/header/form validation retains the explicit application
guard. Invalid header tokens produce HTTP 400 before handler entry, its manual
form read and its no-store header. For a form missing its token, native middleware
records an invalid form feature and the guard's native revalidation throws
`InvalidOperationException` during form access. Tests compare the exact exception
type/message across old-marker, native-marker and MediumAPI hosts, retaining zero
handler entries/manual reads without introducing a catch or replacement result.
Native token validation can read form fields earlier. Valid tokens retain response status, media and
bytes; a valid header on a non-form request reaches the handler's existing
empty HTTP 400 response and no-store header without reading a form. Synthetic
probes record only counts. The public selected and original default entry points
are both exercised, with no analyzer suppression or handler-forwarding lambda.

All prior test sources and assertions remain unchanged. The project now has
50 cases on each existing framework, with no added package, token engine or
middleware. The native reference uses an opaque Delegate-to-MapMethods helper
to preserve the original method and avoid the SDK analyzer's cross-assembly
method-group issue. See the
[runnable example](../RonSijm.MediumAPI.AntiforgeryExample/README.md),
[native annotation guide](../../../../Docs/namespace-tag-defaults.md#use-native-metadata-where-an-application-marker-adds-no-policy) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for scope and executed results. These isolated checks do not exercise a
consuming application's complete security or deployed host.
