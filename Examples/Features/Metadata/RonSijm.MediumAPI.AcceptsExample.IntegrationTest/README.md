# Native request-media integration coverage

This project targets .NET 8 and .NET 10 and references the runnable [Accepts example](../RonSijm.MediumAPI.AcceptsExample/README.md). Its 37 cases per framework use TestServer and an independently composed native host. Both hosts use matching public DTOs and body-reading helpers. The reference host supplies metadata through ordinary native `.Accepts<T>()` and `AcceptsMetadata`, without MediumAPI's mapping/declaration helpers or `Consumes` forwarding.

Checks compare complete ordered `IAcceptsMetadata` values and effective route contracts, then actual API Explorer descriptions including media lists, schema types, requiredness and aliases. Class defaults are attached to an empty native group so their pre-inference position matches the documented endpoint-class position. Separate class and method declarations provide layering; native `Consumes` does not permit repeated attributes on one owner. Filter and later fluent declarations keep their respective native convention positions. No competing schema, optionality difference or unsupported media type is normalized away.

Real HTTP requests verify native JSON binding, malformed JSON rejection, metadata-optional versus parameter-required bodies, manual multipart parsing, raw-body documentation without JSON parsing, native 415 rejection before filters/handlers, same-path content-type selection, group inheritance, aliases and disabled discovery. Native attribute checks cover schema/default values, eager media validation, rejected broad wildcards and the distinction between its mutable public MVC collection and its Minimal API metadata list. A separate assertion verifies that native `ConsumesAttribute` objects themselves are forwarded from class, namespace and registered-filter declarations.

No extra provider package is needed: this suite uses the installed framework's real API Explorer provider. It does not emit or compare full OpenAPI documents, execute MVC filters/action constraints, claim a universal schema-provider policy, replace native media-type selection or add parsing middleware. Wildcard contracts remain available through the native fluent APIs; no new wrapper extends `Consumes` semantics.

From the repository root:

```shell
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.AcceptsExample.IntegrationTest/RonSijm.MediumAPI.AcceptsExample.IntegrationTest.csproj
```

The repository verification record reports executed outcomes. Source-level review and an example's existence alone are not proof that those checks ran.
