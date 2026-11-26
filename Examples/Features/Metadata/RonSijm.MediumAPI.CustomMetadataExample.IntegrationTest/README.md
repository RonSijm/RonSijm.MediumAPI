# Custom-metadata integration coverage

This dual .NET 8/.NET 10 suite contains 28 cases per framework: 16 metadata/construction facts and 12 HTTP cases for the runnable [custom-metadata example](../RonSijm.MediumAPI.CustomMetadataExample/README.md).

The independent native host uses explicit `WithMetadata` values and empty native groups to preserve class metadata's pre-inference position. Native original methods keep their own attributes. The reference applies no MediumAPI custom-attribute reader or synthetic endpoint identity marker. Ordered values, selected route contracts and real API Explorer descriptions compare without removing differences.

Coverage includes constructor/named enum, type, boxed and array values; null params/named values; native-plus-opt-in authorization counted once; filter metadata before response fallbacks; repeated declarations; declared-only owners versus inherited methods; unmarked/disabled throwing declarations; inert static providers; aliases; and fresh host/mount instances and arrays. All sample routes receive actual TestServer requests comparing JSON bodies, statuses and application metadata headers. Repeated mounts suppress default names and prefix the existing native explicit name to retain host uniqueness.

The scope is bounded. Active constructor/setter failures, private nested attribute accessibility, arbitrary mutable-object graphs and additional OpenAPI providers are not comprehensively characterized here. Factory reflection semantics retain the existing library contract; the new interface does not add an activation or validation engine.

```shell
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest/RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest.csproj
```

See the repository verification record for executed outcomes. No consumer runtime or full framework/provider compatibility claim follows from these fixtures.
