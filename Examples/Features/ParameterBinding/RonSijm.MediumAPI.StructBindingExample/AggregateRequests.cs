using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.StructBindingExample;

public struct LocalItemRequest
{
    [FromRoute(Name = "itemId")]
    public long SelectedItem { get; set; }

    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromHeader(Name = SampleMetadata.LabelHeader)]
    public string? Label { get; set; }

    [FromServices]
    public AggregateService Service { get; set; }
}

public struct WorkspaceItemRequest
{
    public WorkspaceItemRequest()
    {
        ParameterlessConstructorUsed = true;
        Page = SampleMetadata.ConstructorPage;
        Label = SampleMetadata.ConstructorLabel;
        Service = null!;
    }

    // Native constructor selection prefers the explicit parameterless constructor.
    public WorkspaceItemRequest(int unused) => throw new InvalidOperationException("The parameterized constructor must not be selected.");

    internal bool ParameterlessConstructorUsed { get; }
    public readonly bool UsedParameterlessConstructor() => ParameterlessConstructorUsed;

    [FromRoute(Name = "customerId")]
    public long AccountId { get; set; }

    [FromRoute(Name = "itemId")]
    public long SelectedItem { get; set; }

    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromHeader(Name = SampleMetadata.LabelHeader)]
    public string? Label { get; set; }

    [FromServices]
    public AggregateService Service { get; set; }
}