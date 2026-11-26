using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public sealed class DocumentPageOptions
{
    public bool Enabled { get; init; } = true;
    public string PublicOrigin { get; init; } = "https://documents.example.test";
}

public sealed class DocumentPageProbe
{
    public int Entries { get; set; }
    public int FileSelections { get; set; }
}

// This is fixed transport fixture content, not a document renderer or asset host.
public sealed class DocumentPageFixture : IDisposable
{
    private readonly object gate = new();
    private readonly string directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mediumapi-document-" + Guid.NewGuid().ToString("N")));
    private bool created;

    public bool Created => created;

    public string SelectFile()
    {
        lock (gate)
        {
            var path = Path.Combine(directory, "document.html");
            if (!created)
            {
                Directory.CreateDirectory(directory);
                using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                file.Write(Encoding.UTF8.GetBytes("<!doctype html><title>Sample document</title><p>Fixed content.</p>"));
                created = true;
            }

            return path;
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (!created) { return; }

            File.Delete(Path.Combine(directory, "document.html"));
            Directory.Delete(directory);
            created = false;
        }
    }
}

public static class DocumentPageEntry
{
    public static Results<RedirectHttpResult, PhysicalFileHttpResult> Enter(HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        probe.Entries++;
        var origin = options.PublicOrigin.TrimEnd('/');
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.IsHttps && string.Equals(context.Request.Host.Host, "edge.documents.example.test", StringComparison.OrdinalIgnoreCase) && !string.Equals(context.Request.Host.Host, new Uri(origin).Host, StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Redirect(origin + context.Request.PathBase + context.Request.Path + context.Request.QueryString);
        }

        probe.FileSelections++;
        return TypedResults.PhysicalFile(fixture.SelectFile(), "text/html; charset=utf-8");
    }

    public static void MapAlias(WebApplication app)
    {
        if (!app.Services.GetRequiredService<DocumentPageOptions>().Enabled) { return; }

        app.MapGroup("/sample").AddEndpointFilter<IngressFilter>().MapMediumApiEndpoint<DocumentPages.PublicDocumentAliasEndpoint>("/document-alias/{publicKey}").WithoutDefaultEndpointName();
    }
}

// These factories preserve selected transport checks without adding response metadata.
public sealed class DocumentProjectGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<long>("customerId", out var customerIndex) || !context.TryGetArgumentIndex<long>("projectId", out var projectIndex))
        {
            throw new InvalidOperationException("DocumentProjectGuardFactory requires long customerId and projectId parameters.");
        }

        return invocation => invocation.GetArgument<long>(customerIndex) > 0 && invocation.GetArgument<long>(projectIndex) > 0 ? next(invocation) : DocumentGuardResults.Missing();
    }
}

public sealed class DocumentCustomerGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<long>("customerId", out var index)) { throw new InvalidOperationException("DocumentCustomerGuardFactory requires a long customerId parameter."); }

        return invocation => invocation.GetArgument<long>(index) > 0 ? next(invocation) : DocumentGuardResults.Missing();
    }
}

public sealed class DocumentControlGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<long>("controlId", out var index)) { throw new InvalidOperationException("DocumentControlGuardFactory requires a long controlId parameter."); }

        return invocation => invocation.GetArgument<long>(index) > 0 ? next(invocation) : DocumentGuardResults.Missing();
    }
}

public sealed class DocumentCustomerKeyGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<long>("customerId", out var customerIndex) || !context.TryGetArgumentIndex<string>("publicKey", out var keyIndex))
        {
            throw new InvalidOperationException("DocumentCustomerKeyGuardFactory requires long customerId and string publicKey parameters.");
        }

        return invocation => invocation.GetArgument<long>(customerIndex) > 0 && Regex.IsMatch(invocation.GetArgument<string>(keyIndex), @"^[a-zA-Z0-9_-]{1,80}$", RegexOptions.CultureInvariant) ? next(invocation) : DocumentGuardResults.Missing();
    }
}

public sealed class DocumentPublicKeyGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<string>("publicKey", out var index)) { throw new InvalidOperationException("DocumentPublicKeyGuardFactory requires a string publicKey parameter."); }

        return invocation => Regex.IsMatch(invocation.GetArgument<string>(index), @"^[a-zA-Z0-9_-]{1,80}$", RegexOptions.CultureInvariant) ? next(invocation) : DocumentGuardResults.Missing();
    }
}

internal static class DocumentGuardResults
{
    internal static ValueTask<object?> Missing()
    {
        Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> result = TypedResults.NotFound();
        return ValueTask.FromResult<object?>(result);
    }
}