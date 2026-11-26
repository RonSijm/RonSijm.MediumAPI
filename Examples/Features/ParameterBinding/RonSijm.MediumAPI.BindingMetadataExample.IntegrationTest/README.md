# Named native header-binding migration checks

The original eight cases retain custom route/query/header/body/service sources,
nullable values, API Explorer and binder precedence on .NET 8 and .NET 10.
`NamedHeaderMigrationTests` adds 14 cases through a separate two-command public
selection, giving 22 cases on each existing framework.

Independent old-native FromHeader, native named-alias and MediumAPI hosts retain
complete route/API Explorer snapshots, raw parameter CLR names/types/positions,
nullable state, defaults and optionality. The alias derives from FromHeaderAttribute
without hiding Name. It keeps IFromHeaderMetadata, IBindingSourceMetadata and
IModelNameProvider, the exact header name/source, and the inherited mutable Name
contract. The header remains a nullable string without a C# default value.

Actual JSON and manually read form commands compare absent, empty, whitespace,
single, multiple, comma-containing and differently cased wire headers. Native
before-host values remain the reference; tests add no trimming, splitting or
normalization. Response status, bytes/media, Location, handler-owned no-store
headers and service values remain equal. Query values do not fill or replace
an explicit header source.

Existing typed/name factory lookup retains the same CLR slot and argument count.
The native reference finds that slot independently through original MethodInfo.
The JSON guard still rejects before handler/service entry and its no-store header;
native malformed-JSON behavior is compared without changing its factory boundary.
Manual form reading remains inside the handler after its content-type check, and
its non-form rejection retains the handler-owned header without reading a form.
No idempotency, replay, token, middleware or body-binding engine is added.

The selected public entry point and original four-route default are exercised.
All older case sources/assertions and project packages remain unchanged. Native
mapping uses opaque original Delegate-to-MapMethods calls rather than wrappers
or analyzer suppression. API Explorer is the available provider; this project
does not generate or compare an OpenAPI document.

See the [runnable example](../RonSijm.MediumAPI.BindingMetadataExample/README.md),
[named header bindings](../../../../Docs/named-header-bindings.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for supported patterns and executed checks. These synthetic requests do not
execute a consuming application's business services or authentication policies.
