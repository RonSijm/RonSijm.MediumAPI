using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.EndpointDocumentationExample.NamedAliases;
using Xunit;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest
{
    internal static class NamedConventionHost
    {
        internal static WebApplication Create(string reference = "named", bool enabled = true, bool alias = false, bool map = true)
        {
            var app = Program.Build(configure: builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Logging.ClearProviders();
                builder.Configuration["Sample:ConventionAliases"] = enabled.ToString();
                builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
            }, mapEndpoints: false);
            RouteGroupBuilder? group = null;
            if (map && reference == "named") { group = app.TryMapMediumApiNamespace<NamedConventionGroupDefinition>(NamedConventionSample.ConfigureMount); }
            else if (map && reference == "generic") { group = app.TryMapMediumApiNamespace<GenericAliasRoutes.GenericConventionGroupDefinition>(NamedConventionSample.ConfigureMount); }
            else if (map && enabled) { group = MapNative(app); }

            if (group is not null && alias)
            {
                if (reference == "named") { group.MapMediumApiEndpoint<GetNamedConventionValueEndpoint>("/alias").WithName("NamedConventionAlias"); }
                else if (reference == "generic") { group.MapMediumApiEndpoint<GenericAliasRoutes.GenericValueEndpoint>("/alias").WithName("NamedConventionAlias"); }
                else { NativeValuePolicies(MapOriginal(group, "/alias", NativeValue)).WithName("NamedConventionAlias"); }
            }

            return app;
        }

        internal static void MapFailure(WebApplication app, string reference = "named")
        {
            if (reference == "named") { app.MapMediumApiEndpoint<NamedFailureEndpoint>(); }
            else if (reference == "generic") { app.MapMediumApiEndpoint<GenericFailureEndpoint>(); }
            else { AliasThrowConvention.Configure(MapOriginal(app, "/named-failure", FailureBody)); }
        }

        internal static IEnumerable<RouteEndpoint> Routes(WebApplication app)
        {
            return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
        }

        internal static RouteEndpoint Route(WebApplication app, string pattern)
        {
            return Assert.Single(Routes(app), route => route.RoutePattern.RawText == pattern);
        }

        internal static AliasConventionProbe Probe(WebApplication app)
        {
            return app.Services.GetRequiredService<AliasConventionProbe>();
        }

        internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
        {
            return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
        }

        internal static string OpenApi(WebApplication app, string document = DocumentationGroups.Public)
        {
            return Program.GetOpenApiJson(app.Services, document);
        }

        private static RouteGroupBuilder MapNative(WebApplication app)
        {
            var group = app.MapGroup("/named-conventions").WithMetadata(new AliasConventionStage("map")).WithTags("Named convention aliases").WithGroupName(DocumentationGroups.Public);
            group.AddEndpointFilter(async (context, next) =>
            {
                var probe = context.HttpContext.RequestServices.GetRequiredService<AliasConventionProbe>();
                probe.RecordEvent("guard-enter");
                if (context.HttpContext.Request.Headers.ContainsKey("X-Reject"))
                {
                    probe.RecordEvent("guard-reject");
                    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
                }

                var result = await next(context);
                probe.RecordEvent("guard-exit");
                return result;
            });
            NativePhase(group, "namespace", "X-Alias-Scope", final: true);
            NativePhase(group, "host", final: true);
            NativeMethodPolicies(MapOriginal(group, "/inherited", NativeInherited), 1).WithName("GetNamedConventionInherited");
            NativeMethodPolicies(MapOriginal(group, "/overridden", NativeOverridden), OldOverrideMethodConventionCount()).WithName("GetNamedConventionOverridden");
            var repeated = NativeMethodPolicies(MapOriginal(group, "/repeated", NativeRepeated), 2).WithName("GetNamedConventionRepeated");
            NativePhase(repeated, "explicit-struct");
            NativePhase(repeated, "explicit-class");
            NativeValuePolicies(MapOriginal(group, "/value", NativeValue)).WithName("GetNamedConventionValue");
            return group;
        }

        private static int OldOverrideMethodConventionCount()
        {
            var declarations = typeof(GenericAliasRoutes.GenericOverriddenEndpoint).GetMethod("HandleAsync")!.GetCustomAttributes(inherit: true).OfType<EndpointConventionAttribute>().ToArray();
            foreach (var declaration in declarations)
            {
                if (declaration.GetType() != typeof(EndpointConventionAttribute<AliasMethodConvention>)) { throw new InvalidOperationException("The old override fixture must declare only the expected fixed generic method convention."); }
            }

            return declarations.Length;
        }

        private static RouteHandlerBuilder NativeValuePolicies(RouteHandlerBuilder builder)
        {
            NativePhase(builder, "class", "X-Alias-Class");
            return NativeMethodPolicies(builder, 1);
        }

        private static RouteHandlerBuilder NativeMethodPolicies(RouteHandlerBuilder builder, int count)
        {
            for (var index = 0; index < count; index++) { NativePhase(builder, "method", "X-Alias-Method"); }

            return builder;
        }

        private static void NativePhase<TBuilder>(TBuilder builder, string stage, string? header = null, bool final = false) where TBuilder : IEndpointConventionBuilder
        {
            builder.Add(endpoint =>
            {
                endpoint.Metadata.Add(new AliasConventionStage(stage));
                endpoint.ApplicationServices.GetRequiredService<AliasConventionProbe>().RecordMaterialization(stage);
            });
            if (header is not null)
            {
                builder.AddEndpointFilter((context, next) =>
                {
                    context.HttpContext.Response.Headers[header] = stage;
                    return next(context);
                });
            }

            if (final)
            {
                builder.Finally(endpoint =>
                {
                    endpoint.Metadata.Add(new AliasConventionStage(stage + "-finally"));
                    endpoint.ApplicationServices.GetRequiredService<AliasConventionProbe>().RecordMaterialization(stage + "-finally");
                });
            }
        }

        private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
        {
            return endpoints.MapMethods(pattern, new[] { HttpMethods.Get }, handler);
        }

        private static Ok<AliasConventionReply> NativeValue([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            context.Response.Headers["X-Alias-Handler"] = "handler";
            return AliasConventionBodies.Create("value", page, context, probe);
        }

        private static Ok<AliasConventionReply> NativeRepeated([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            context.Response.Headers["X-Alias-Handler"] = "handler";
            return AliasConventionBodies.Create("repeated", page, context, probe);
        }

        private static Ok<AliasConventionReply> NativeInherited([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            context.Response.Headers["X-Alias-Handler"] = "handler";
            return AliasConventionBodies.Create("inherited", page, context, probe);
        }

        private static Ok<AliasConventionReply> NativeOverridden([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            context.Response.Headers["X-Alias-Handler"] = "handler";
            return AliasConventionBodies.Create("overridden", page, context, probe);
        }

        private static Ok<string> FailureBody()
        {
            return TypedResults.Ok("unreachable");
        }
    }

    [NamedFailure]
    public sealed class NamedFailureEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/named-failure";

        public Ok<string> HandleAsync()
        {
            return TypedResults.Ok("unreachable");
        }
    }

    [EndpointConvention<AliasThrowConvention>]
    public sealed class GenericFailureEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/named-failure";

        public Ok<string> HandleAsync()
        {
            return TypedResults.Ok("unreachable");
        }
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.GenericAliasRoutes
{
    [EndpointFilter<AliasConventionGuard>]
    [EndpointConvention<AliasScopeConvention>]
    [Tags("Named convention aliases")]
    [EndpointGroupName(DocumentationGroups.Public)]
    public sealed class GenericConventionGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Configuration.GetValue("Sample:ConventionAliases", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/named-conventions").WithMetadata(new AliasConventionStage("map"));
        }
    }

    [EndpointConvention<AliasClassConvention>]
    public sealed class GenericValueEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EndpointName("GetNamedConventionValue")]
        [EndpointConvention<AliasMethodConvention>]
        [ResponseHeader("X-Alias-Handler", "handler")]
        public Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            return AliasConventionBodies.Create("value", page, context, probe);
        }
    }

    public sealed class GenericRepeatedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/repeated";

        [EndpointName("GetNamedConventionRepeated")]
        [EndpointConvention<AliasMethodConvention>]
        [EndpointConvention<AliasMethodConvention>]
        [EndpointConvention<AliasStructConvention>]
        [EndpointConvention<AliasExplicitClassConvention>]
        [ResponseHeader("X-Alias-Handler", "handler")]
        public Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            return AliasConventionBodies.Create("repeated", page, context, probe);
        }
    }

    [EndpointConvention<AliasClassConvention>]
    public abstract class GenericConventionMethodBase : IGetEndpoint
    {
        public abstract string Pattern { get; }

        [EndpointName("GetNamedConventionInherited")]
        [EndpointConvention<AliasMethodConvention>]
        [ResponseHeader("X-Alias-Handler", "handler")]
        public virtual Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            return AliasConventionBodies.Create("inherited", page, context, probe);
        }
    }

    public sealed class GenericInheritedEndpoint : GenericConventionMethodBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/inherited";
    }

    public sealed class GenericOverriddenEndpoint : GenericConventionMethodBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/overridden";

        [EndpointName("GetNamedConventionOverridden")]
        [EndpointConvention<AliasMethodConvention>]
        [ResponseHeader("X-Alias-Handler", "handler")]
        public override Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
        {
            return AliasConventionBodies.Create("overridden", page, context, probe);
        }
    }
}