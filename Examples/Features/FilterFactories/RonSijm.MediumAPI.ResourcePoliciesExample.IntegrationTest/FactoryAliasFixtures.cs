using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.FactoryAliasFixtures
{
    public sealed record FactoryAliasReceipt(long CustomerId, int? Take, int Count);

    [EndpointResponse(499)]
    [ResponseHeader("X-Ignored-Alias", "ignored")]
    [UnrelatedAliasDecoration]
    public sealed class NamedDeclaredAttribute : EndpointFilterFactoryAttribute<DeclaredFactory> { }

    public sealed class NamedRepeatedAttribute : EndpointFilterFactoryAttribute<RepeatedFactory> { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedAliasDecorationAttribute : Attribute
    {
        public UnrelatedAliasDecorationAttribute()
        {
            throw new InvalidOperationException("Factory alias decorations must not be imported.");
        }
    }

    public static class FactoryAliasHost
    {
        public static WebApplication Create(string reference = "named", bool enabled = true, bool alias = false)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(FactoryAliasHost).Assembly.FullName, EnvironmentName = "Testing" });
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Sample:FactoryAliases"] = enabled.ToString() });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddScoped<FactoryRequestScope>();
            builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
            var app = builder.Build();
            RouteGroupBuilder? group;
            if (reference == "named") { group = app.TryMapMediumApiNamespace<Named.GroupDefinition>(); }
            else if (reference == "generic") { group = app.TryMapMediumApiNamespace<Generic.GroupDefinition>(); }
            else
            {
                if (!enabled) { return app; }

                group = app.MapGroup("/factory-alias").WithTags("Factory aliases");
                ((IEndpointConventionBuilder)group).Add(endpoint => endpoint.Metadata.Add(new FactoryStageAttribute("configure")));
                group.WithMetadata(new FactoryStageAttribute("declared"), new ConsumesAttribute(typeof(GuardedStartRequest), "application/json"));
                group.DisableAntiforgery();
                group.WithMetadata(new EndpointResponseAttribute(418));
                ((IEndpointConventionBuilder)group).Finally(endpoint =>
                {
                    if (!endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == 419)) { endpoint.Metadata.Add(new EndpointResponseAttribute(419)); }
                });
                group.AddEndpointFilterFactory((_, next) => async invocation =>
                {
                    invocation.HttpContext.Response.Headers["X-Factory-Declaration"] = "present";
                    return await next(invocation);
                });
                group.AddEndpointFilterFactory(DeclaredFactory.Create);
            }

            if (group is null) { return app; }

            group.AddEndpointFilterFactory((_, next) => invocation =>
            {
                if (invocation.HttpContext.Request.Headers.ContainsKey("X-Replace-Customer")) { invocation.Arguments[0] = 7L; }

                return next(invocation);
            });
            if (reference == "named")
            {
                group.MapMediumApiEndpoint<Named.ValueEndpoint>().WithName("FactoryAliasValue");
                if (alias) { group.MapMediumApiEndpoint<Named.ValueEndpoint>("/alias/{customerId:long}").WithoutDefaultEndpointName(); }
            }
            else if (reference == "generic")
            {
                group.MapMediumApiEndpoint<Generic.ValueEndpoint>().WithName("FactoryAliasValue");
                if (alias) { group.MapMediumApiEndpoint<Generic.ValueEndpoint>("/alias/{customerId:long}").WithoutDefaultEndpointName(); }
            }
            else
            {
                NativeEndpoint(group, "/value/{customerId:long}").WithName("FactoryAliasValue");
                if (alias) { NativeEndpoint(group, "/alias/{customerId:long}"); }
            }

            return app;
        }

        private static RouteHandlerBuilder NativeEndpoint(RouteGroupBuilder group, string pattern)
        {
            var endpoint = group.MapGet(pattern, (Delegate)(Func<long, int?, HttpContext, FactoryRequestScope, Ok<FactoryAliasReceipt>>)HandleAsync);
            endpoint.AddEndpointFilterFactory(RepeatedFactory.Create);
            endpoint.AddEndpointFilterFactory(RepeatedFactory.Create);
            endpoint.AddEndpointFilterFactory(DocumentCustomerGuardFactory.Create);
            return endpoint;
        }

        private static Ok<FactoryAliasReceipt> HandleAsync([FromRoute] long customerId, [FromQuery] int? take, HttpContext context, FactoryRequestScope scope)
        {
            context.Response.Headers["X-Handler"] = "entered";
            context.Response.Headers["X-Request-Scope"] = scope.Id.ToString();
            return Body(customerId, take, context);
        }

        public static Ok<FactoryAliasReceipt> Body(long customerId, int? take, HttpContext context)
        {
            var count = context.Items.TryGetValue("factory-count", out var current) ? (int)current! : 0;
            return TypedResults.Ok(new FactoryAliasReceipt(customerId, take, count));
        }
    }

    public abstract class NamedMethodBase : IGetEndpoint, IManuallyMappedEndpoint
    {
        public abstract string Pattern { get; }

        [NamedRepeated]
        public virtual Ok<int> HandleAsync(HttpContext context)
        {
            return TypedResults.Ok(context.Items.TryGetValue("factory-count", out var current) ? (int)current! : 0);
        }
    }

    public sealed class NamedOverrideEndpoint : NamedMethodBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/override";

        [NamedRepeated]
        public override Ok<int> HandleAsync(HttpContext context)
        {
            return TypedResults.Ok(context.Items.TryGetValue("factory-count", out var current) ? (int)current! : 0);
        }
    }

    public abstract class GenericMethodBase : IGetEndpoint, IManuallyMappedEndpoint
    {
        public abstract string Pattern { get; }

        [EndpointFilterFactory<RepeatedFactory>]
        public virtual Ok<int> HandleAsync(HttpContext context)
        {
            return TypedResults.Ok(context.Items.TryGetValue("factory-count", out var current) ? (int)current! : 0);
        }
    }

    public sealed class GenericOverrideEndpoint : GenericMethodBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/override";

        [EndpointFilterFactory<RepeatedFactory>]
        public override Ok<int> HandleAsync(HttpContext context)
        {
            return TypedResults.Ok(context.Items.TryGetValue("factory-count", out var current) ? (int)current! : 0);
        }
    }

    public sealed class FailingConfigureFactory : IMediumApiEndpointFilterFactory
    {
        private FailingConfigureFactory()
        {
            throw new InvalidOperationException("Factory instances must not be created.");
        }

        public static InvalidOperationException Failure { get; } = new("Fixed alias Configure failure.");

        public static void Configure(IEndpointConventionBuilder endpoint)
        {
            throw Failure;
        }

        public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
        {
            return next;
        }
    }

    public sealed class FailingCreateFactory : IMediumApiEndpointFilterFactory
    {
        public static InvalidOperationException Failure { get; } = new("Fixed alias Create failure.");

        public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
        {
            throw Failure;
        }
    }

    public sealed class NamedFailingConfigureAttribute : EndpointFilterFactoryAttribute<FailingConfigureFactory> { }

    public sealed class NamedFailingCreateAttribute : EndpointFilterFactoryAttribute<FailingCreateFactory> { }

    [NamedFailingConfigure]
    public sealed class FailingConfigureEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/configure-failure";

        public Ok<int> HandleAsync()
        {
            return TypedResults.Ok(1);
        }
    }

    [NamedFailingCreate]
    public sealed class FailingCreateEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/create-failure";

        public Ok<int> HandleAsync()
        {
            return TypedResults.Ok(1);
        }
    }
}

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.FactoryAliasFixtures.Named
{
    [NamedDeclared]
    public sealed class GroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Configuration.GetValue("Sample:FactoryAliases", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/factory-alias").WithTags("Factory aliases");
        }
    }

    [NamedRepeated]
    public sealed class ValueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value/{customerId:long}";

        [NamedRepeated]
        [DocumentCustomerGuard]
        [ResponseHeader("X-Handler", "entered")]
        public Ok<FactoryAliasReceipt> HandleAsync([FromRoute] long customerId, [FromQuery] int? take, HttpContext context, FactoryRequestScope scope)
        {
            context.Response.Headers["X-Request-Scope"] = scope.Id.ToString();
            return FactoryAliasHost.Body(customerId, take, context);
        }
    }
}

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.FactoryAliasFixtures.Generic
{
    [EndpointFilterFactory<DeclaredFactory>]
    public sealed class GroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Configuration.GetValue("Sample:FactoryAliases", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/factory-alias").WithTags("Factory aliases");
        }
    }

    [EndpointFilterFactory<RepeatedFactory>]
    public sealed class ValueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value/{customerId:long}";

        [EndpointFilterFactory<RepeatedFactory>]
        [EndpointFilterFactory<DocumentCustomerGuardFactory>]
        [ResponseHeader("X-Handler", "entered")]
        public Ok<FactoryAliasReceipt> HandleAsync([FromRoute] long customerId, [FromQuery] int? take, HttpContext context, FactoryRequestScope scope)
        {
            context.Response.Headers["X-Request-Scope"] = scope.Id.ToString();
            return FactoryAliasHost.Body(customerId, take, context);
        }
    }
}