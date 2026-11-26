# Application-owned metadata attributes

This runnable .NET 8/.NET 10 example opts an application attribute into class, namespace and registered-filter metadata forwarding:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AppMetadataAttribute : Attribute, IEndpointMetadataAttribute
{
    public AppMetadataAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
```

`ReadMetadataFilter` reads those ordinary endpoint metadata objects and writes their labels to `X-Sample-Metadata`. Handlers return the same labels in typed JSON replies. The application owns their meaning; the marker interface adds no runtime policy, MVC filter execution or static-provider dispatch.

All routes below use `GET`.

| Route | Demonstrates |
| --- | --- |
| `/metadata/class` | Class metadata with constructor types, boxed enums, arrays, named properties and an enum field. |
| `/metadata/method` | Original handler metadata, including an attribute without the opt-in interface. |
| `/metadata/filter` | Optional filter `Configure`, opted-in metadata, native authorization data and custom response metadata before a fallback. |
| `/metadata/order` | Namespace Map/declaration, class, inferred provider, method, endpoint Configure and final convention order. |
| `/metadata/inherited` | Direct-only class/group/filter declarations and an inherited original handler. |
| `/metadata/repeated` | Two actual attribute declarations remain two values. |
| `/metadata/null` | Null constructor arguments, null params array and null named values. |
| `/metadata/static` | An attribute implementing `IEndpointMetadataProvider` remains stored metadata; its throwing static method is not invoked. |
| `/metadata/dual` | A native authorization attribute also opting in is appended once. |
| `/metadata/native` | Namespace declarations reach an ordinary native route created in `Map`. |
| `/aliases/first`, `/aliases/second` | Explicit manual aliases retain class metadata without importing the automatic namespace's declarations or filter. |

The disabled namespace contains deliberately throwing opted-in declarations and maps no routes. Unmarked throwing class attributes demonstrate selective construction. The filter and dual fixtures explicitly use native `[AllowAnonymous]`, so they remain public while authorization metadata composition is inspected. The host registers the native policy and middleware explicitly.

Class and namespace lookup remains direct-only. Original method attributes retain native inheritance and require no new interface. Each selected declaration creates an attribute with its constructor and named arguments; its own mutability and constructor/setter behavior remain application-owned. The example checks independent instance/array construction across hosts and mounts without promising that arbitrary metadata objects are immutable.

The companion suite compares native `WithMetadata` registrations, ordered attribute values, complete selected route contracts, real API Explorer descriptions and HTTP payload/header behavior. It contains 28 cases per framework, including actual requests to all 12 routes. It adds no OpenAPI provider package or diagnostic endpoint.

```shell
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample --framework net8.0
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest/RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest.csproj
```

See [custom metadata](../../../../Docs/custom-metadata.md) for the complete forwarding contract and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed results. The case count and source files alone are not test-run evidence or a claim about every custom attribute/provider.
