using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.StartupFixtures
{
    public readonly struct StructConvention : IMediumApiEndpointConvention
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("struct")));
        }
    }

    public sealed class ExplicitConvention : IMediumApiEndpointConvention
    {
        static void IMediumApiEndpointConvention.Configure(IEndpointConventionBuilder builder)
        {
            builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("explicit")));
        }
    }

    public sealed class MethodConvention : IMediumApiEndpointConvention
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("method")));
        }
    }

    public sealed class ClassConvention : IMediumApiEndpointConvention
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("class")));
        }
    }

    public sealed class TraceFilter : IMediumApiEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("filter")));
        }

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            return next(context);
        }
    }

    public sealed class RepeatedConvention : IMediumApiEndpointConvention
    {
        public static int Configurations { get; private set; }

        public static void Reset()
        {
            Configurations = 0;
        }

        public static void Configure(IEndpointConventionBuilder builder)
        {
            Configurations++;
            builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("repeat")));
        }
    }

    [EndpointConvention<RepeatedConvention>]
    [EndpointConvention<RepeatedConvention>]
    public sealed class RepeatedConventionEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/repeat";

        public Ok<string> HandleAsync()
        {
            return TypedResults.Ok("fixed");
        }
    }

    public sealed class ThrowingConvention : IMediumApiEndpointConvention
    {
        public static InvalidOperationException Failure { get; } = new("A fixed startup convention failure.");

        public static void Configure(IEndpointConventionBuilder builder)
        {
            throw Failure;
        }
    }

    [EndpointConvention<ThrowingConvention>]
    public sealed class ThrowingConventionEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/throwing-convention";

        public Ok<string> HandleAsync()
        {
            return TypedResults.Ok("fixed");
        }
    }

    [EndpointResponse(418)]
    [ResponseHeader("X-Unused-Convention", "ignored")]
    [UnrelatedConvention]
    public sealed class NoImplicitMetadataConvention : IMediumApiEndpointConvention
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("explicit-only")));
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedConventionAttribute : Attribute
    {
        public UnrelatedConventionAttribute()
        {
            throw new InvalidOperationException("Unrelated convention attributes must not be constructed.");
        }
    }

    [EndpointConvention<ThrowingConvention>]
    public abstract class DecoratedEndpointBase : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EndpointConvention<MethodConvention>]
        public Ok<string> HandleAsync()
        {
            return TypedResults.Ok("fixed");
        }
    }

    public sealed class InheritedConventionEndpoint : DecoratedEndpointBase;

    [EndpointConvention<ThrowingConvention>]
    public abstract class DecoratedGroupBase;

    [EndpointConvention<ClassConvention>]
    [EndpointFilter<TraceFilter>]
    [EndpointConvention<StructConvention>]
    public sealed class OrderedConventionEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EndpointFilter<TraceFilter>]
        [EndpointConvention<MethodConvention>]
        public Ok<string> HandleAsync()
        {
            return TypedResults.Ok("fixed");
        }
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.StartupFixtures.Disabled
{
    [EndpointConvention<global::RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.StartupFixtures.ThrowingConvention>]
    public sealed class DisabledConventionGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("Disabled namespace mapping must not run.");
        }
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.StartupFixtures.Inheritance
{
    public sealed class InheritedConventionGroupDefinition : global::RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.StartupFixtures.DecoratedGroupBase, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/inherited-conventions");
        }
    }
}