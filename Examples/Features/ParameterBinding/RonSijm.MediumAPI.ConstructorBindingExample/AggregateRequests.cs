using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ConstructorBindingExample;

public sealed record LocalItemRequest(
    [param: FromRoute(Name = "itemId")] long SelectedItem,
    [param: FromServices] AggregateService Service,
    [param: FromQuery(Name = "page")] int? Page = SampleMetadata.DefaultPage,
    [param: FromHeader(Name = SampleMetadata.LabelHeader)]
    string? Label = null);

public sealed class WorkspaceItemRequest
{
    public WorkspaceItemRequest(long accountId, long selectedItem, AggregateService service, int? page = SampleMetadata.DefaultPage, string? label = null)
    {
        AccountId = accountId;
        SelectedItem = selectedItem;
        Service = service;
        Page = page;
        Label = label;
    }

    [FromRoute(Name = "customerId")]
    public long AccountId { get; }

    [FromRoute(Name = "itemId")]
    public long SelectedItem { get; }

    [FromServices]
    public AggregateService Service { get; }

    [FromQuery(Name = "page")]
    public int? Page { get; }

    [FromHeader(Name = SampleMetadata.LabelHeader)]
    public string? Label { get; }
}