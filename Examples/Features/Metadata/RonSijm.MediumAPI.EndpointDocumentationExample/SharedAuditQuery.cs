using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointDocumentationExample;

// Keep the original CLR names as well as wire aliases: native parse errors use the CLR name.
public sealed class SharedAuditQuery
{
    public SharedAuditQuery() { }

    [FromQuery(Name = "search")]
    public string? search { get; set; }

    [FromQuery(Name = "customerId")]
    public long? customerId { get; set; }

    [FromQuery(Name = "platform")]
    public bool? platform { get; set; }

    [FromQuery(Name = "action")]
    public string? action { get; set; }

    [FromQuery(Name = "entityType")]
    public string? entityType { get; set; }

    [FromQuery(Name = "userId")]
    public long? userId { get; set; }

    [FromQuery(Name = "from")]
    public string? from { get; set; }

    [FromQuery(Name = "to")]
    public string? to { get; set; }

    [FromQuery(Name = "page")]
    public int? page { get; set; }

    [FromQuery(Name = "pageSize")]
    public int? pageSize { get; set; }

    [FromQuery(Name = "projectId")]
    public long? projectId { get; set; }

    [FromQuery(Name = "kind")]
    public string? kind { get; set; }
}