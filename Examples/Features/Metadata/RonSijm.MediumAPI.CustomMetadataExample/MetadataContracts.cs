using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace RonSijm.MediumAPI.CustomMetadataExample;

public enum MetadataMode
{
    None,
    First,
    Second
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AppMetadataAttribute : Attribute, IEndpointMetadataAttribute
{
    public AppMetadataAttribute(string name) : this(name, null, null, []) { }

    public AppMetadataAttribute(string name, Type? contract, object? boxed, params MetadataMode[]? modes)
    {
        Name = name;
        Contract = contract;
        Boxed = boxed;
        Modes = modes;
    }

    public string Name { get; }
    public Type? Contract { get; }
    public object? Boxed { get; }
    public MetadataMode[]? Modes { get; }
    public string[]? Aliases { get; set; }
    public object? Extra { get; set; }
    public string? Optional { get; set; }
    public MetadataMode Flag;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class SampleAccessAttribute : AuthorizeAttribute, IEndpointMetadataAttribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class UnmarkedMethodMetadataAttribute : Attribute
{
    public UnmarkedMethodMetadataAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class UnmarkedThrowingAttribute : Attribute
{
    public UnmarkedThrowingAttribute()
    {
        throw new InvalidOperationException("Unmarked class attributes must remain unconstructed.");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class SkippedMetadataAttribute : Attribute, IEndpointMetadataAttribute
{
    public SkippedMetadataAttribute()
    {
        throw new InvalidOperationException("Disabled metadata must remain unconstructed.");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class StaticMetadataAttribute : Attribute, IEndpointMetadataAttribute, IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        throw new InvalidOperationException("An attribute stored as metadata is not a return/parameter provider.");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomResponseAttribute : Attribute, IEndpointMetadataAttribute, IProducesResponseTypeMetadata
{
    public int StatusCode => StatusCodes.Status404NotFound;
    public Type? Type => typeof(MetadataReply);
    public IEnumerable<string> ContentTypes => ["application/x-custom-error"];
}

public sealed record MetadataReply(string Source, string[] Labels);

public sealed class MetadataProbe
{
    private int targetActivations;
    private int calls;
    public int TargetActivations => Volatile.Read(ref targetActivations);
    public int Calls => Volatile.Read(ref calls);

    public void ActivateTarget()
    {
        Interlocked.Increment(ref targetActivations);
    }

    public void Call()
    {
        Interlocked.Increment(ref calls);
    }
}

public static class MetadataReplies
{
    public static string[] Labels(HttpContext context)
    {
        return context.GetEndpoint()!.Metadata.GetOrderedMetadata<AppMetadataAttribute>().Select(metadata => metadata.Name).ToArray();
    }

    public static Ok<MetadataReply> Ok(string source, HttpContext context)
    {
        context.RequestServices.GetRequiredService<MetadataProbe>().Call();
        return TypedResults.Ok(new MetadataReply(source, Labels(context)));
    }

    public static Ok<MetadataReply> Native(HttpContext context)
    {
        return Ok("native", context);
    }
}

public sealed class MetadataProviderResult : IResult, IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new AppMetadataAttribute("provider"));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(200, typeof(MetadataReply), ["application/json"]));
    }

    public Task ExecuteAsync(HttpContext context)
    {
        return MetadataReplies.Ok("order", context).ExecuteAsync(context);
    }
}