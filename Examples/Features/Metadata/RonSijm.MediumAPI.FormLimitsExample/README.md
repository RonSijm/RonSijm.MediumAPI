# Native form limits

This .NET 8/.NET 10 sample uses native `RequestFormLimitsAttribute` declarations on namespace definitions and endpoint classes. ASP.NET Core's `FormFeature` parses fields and files; MediumAPI forwards the supported metadata without adding a parser, byte counter, validation middleware or MVC action-filter execution.

## Routes

All routes accept `POST`. The small limits make the example behavior visible without large payloads.

| Route | Form policy |
| --- | --- |
| `/forms/group` | Namespace declaration: three entries, URLencoded key length 12, URLencoded value length 16 and multipart section body length 32. |
| `/forms/class` | Class declaration replaces those settings with two entries, key length 8 and value length 8; unassigned fields return to native defaults. |
| `/forms/method` | The original handler's native declaration sets four entries, key length 10 and value length 12. |
| `/forms/configured` | Native `WithFormOptions(valueLengthLimit: 4)` changes only that property after the class declaration. |
| `/forms/final` | A native `Finally` convention supplies the last complete attribute: two entries, key length 8 and value length 2. |
| `/forms/reset` | A class attribute setting only `ValueCountLimit = 2` also resets the other properties to native `FormOptions` defaults. |
| `/forms/sparse` | Sparse fluent metadata replaces value length with 4 while retaining the namespace's other settings. |
| `/forms/file` | Native `IFormFile` binding with a 32-byte multipart section limit. |
| `/open` | No endpoint form-options metadata: host-owned `FormOptions` supplies one entry, key length 5, value length 5 and multipart section length 16. |

`IFormCollection` and `IFormFile` remain ordinary native handler parameters. Successful replies contain the parsed field/file shape, and another `ReadFormAsync` observes the same cached form. The request-owned group endpoint is disposable; its resources are released without disposing injected services.

## Full attributes and sparse conventions

`RequestFormLimitsAttribute` implements `IFormOptionsMetadata`, but every getter returns its current native value, including defaults for properties that were not written in the declaration. A later attribute can therefore reset earlier properties. Native `WithFormOptions` instead records unspecified properties as null, allowing native property-wise merging to retain earlier values. The attribute's MVC `Order` does not reorder Minimal API metadata or invoke MVC filters.

These parsers have different limits. URLencoded fields use `KeyLengthLimit` and `ValueLengthLimit`. Native multipart parsing uses its boundary, header, section and entry limits; this example characterizes a multipart field accepted under its section limit even though the same URLencoded key/value pair exceeds those parser limits. `MultipartBodyLengthLimit` applies per section: two accepted 32-byte file sections can exceed 32 bytes in total. It does not replace the server's total request-body limit, proxy policies or application rules.

This isolated parsing sample explicitly calls native `DisableAntiforgery` on its routes. That sample choice is separate from form-limit metadata. A browser-facing application must choose its own appropriate security policy; MediumAPI does not infer an antiforgery opt-out from a form-limit declaration. Separate scope checks retain the native inferred requirement and missing-middleware behavior.

## Run and verify

From the repository root:

```shell
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.FormLimitsExample --framework net10.0
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.FormLimitsExample.IntegrationTest
```

The companion tests independently map native reference routes with the same names, typed results and metadata. They compare ordered form options, raw route/API Explorer snapshots and actual TestServer HTTP requests. Cases cover URLencoded boundaries, multipart section limits, parser differences, native 400/415 responses, property reset vs sparse merging, cached form identity and owned cleanup. Scope fixtures separately cover discovery, ordering, class inheritance, exact native attribute selection, antiforgery and native feature timing.

TestServer exercises the real form parser here. It does not prove Kestrel's total body-byte enforcement; the separate request-body-limits sample covers that behavior. No automatic error-response metadata, request-body policy or form-feature replacement is added by this feature. See the [form-limit guide](../../../../Docs/form-limits.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed results and remaining boundaries.
