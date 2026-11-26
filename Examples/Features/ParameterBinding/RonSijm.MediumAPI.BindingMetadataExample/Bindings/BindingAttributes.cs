using Microsoft.AspNetCore.Http.Metadata;

namespace RonSijm.MediumAPI.BindingMetadataExample.Bindings;

// These attributes belong to the sample application. The framework executes their
// metadata getters during normal binding; the analyzer does not execute them.
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ProductIdRouteAttribute : Attribute, IFromRouteMetadata
{
    public string? Name => "id";
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PageQueryAttribute : Attribute, IFromQueryMetadata
{
    public string? Name => "page";
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class RevisionHeaderAttribute : Attribute, IFromHeaderMetadata
{
    public const string HeaderName = "X-Sample-Revision";
    public string? Name => HeaderName;
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class JsonBodyAttribute : Attribute, IFromBodyMetadata
{
    public bool AllowEmpty => false;
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class CatalogServiceAttribute : Attribute, IFromServiceMetadata { }

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SelectionHeaderAttribute : Attribute, IFromHeaderMetadata
{
    public const string HeaderName = "X-Sample-Selection";
    public string? Name => HeaderName;
}