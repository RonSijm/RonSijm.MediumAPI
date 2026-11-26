using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AsParametersExample;

public sealed class LocalItemRequest
{
    public LocalItemRequest() { }

    [FromRoute(Name = "itemId")]
    public long SelectedItem { get; set; }

    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromHeader(Name = SampleMetadata.LabelHeader)]
    public string? Label { get; set; }

    [FromServices]
    public AggregateService Service { get; set; } = null!;
}

public sealed class WorkspaceItemRequest
{
    public WorkspaceItemRequest() { }

    [FromRoute(Name = "customerId")]
    public long AccountId { get; set; }

    [FromRoute(Name = "itemId")]
    public long SelectedItem { get; set; }

    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromHeader(Name = SampleMetadata.LabelHeader)]
    public string? Label { get; set; }

    [FromServices]
    public AggregateService Service { get; set; } = null!;
}