using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class NativeRateLimitingNamespaceRouteTests
{
    private const string Imports = """
                                   #nullable enable
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.RateLimiting;
                                   using Microsoft.AspNetCore.Routing;
                                   using RonSijm.MediumAPI;

                                   """;

    private const string PolicyMembers = """
                                         private static string PolicyName => throw new InvalidOperationException("Analysis must not read a policy name.");
                                         private static IRateLimiterPolicy<string> Policy => throw new InvalidOperationException("Analysis must not read a policy.");
                                         private static IRateLimiterPolicy<int> IntPolicy => throw new InvalidOperationException("Analysis must not read a policy.");
                                         private static IRateLimiterPolicy<string> CreatePolicy() => throw new InvalidOperationException("Analysis must not invoke a policy factory.");
                                         private static string CreatePolicyName() => throw new InvalidOperationException("Analysis must not invoke a policy-name factory.");
                                         """;

    [Theory]
    [InlineData(".RequireRateLimiting(\"Customers\")")]
    [InlineData(".RequireRateLimiting(Policy)")]
    [InlineData(".DisableRateLimiting()")]
    [InlineData(".RequireRateLimiting<RouteGroupBuilder>(\"Customers\")")]
    [InlineData(".RequireRateLimiting<RouteGroupBuilder, int>(IntPolicy)")]
    [InlineData(".DisableRateLimiting<RouteGroupBuilder>()")]
    public async Task Exact_native_rate_limiting_overloads_retain_the_declared_constraint_advisory(string convention)
    {
        AssertInfo(await Analyze(Source("=> endpoints.MapGroup(\"/api/{customerId:long}\")" + convention + ";", "[FromRoute] int customerId")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    [Theory]
    [InlineData(".RequireRateLimiting(\"Customers\")")]
    [InlineData(".RequireRateLimiting(Policy)")]
    [InlineData(".DisableRateLimiting()")]
    public async Task Matching_inherited_FromRoute_aliases_remain_valid_through_each_native_overload(string convention)
    {
        Assert.Empty(await Analyze(Source("=> endpoints.MapGroup(\"/api/{customerId:long}\")" + convention + ";", "[FromRoute(Name = \"customerId\")] long accountId")));
    }

    [Theory]
    [InlineData(".RequireRateLimiting(\"Customers\")")]
    [InlineData(".RequireRateLimiting(Policy)")]
    [InlineData(".DisableRateLimiting()")]
    public async Task An_absent_inherited_binding_remains_an_information_advisory(string convention)
    {
        AssertInfo(await Analyze(Source("=> endpoints.MapGroup(\"/api/{customerId:long}\")" + convention + ";", "[FromRoute] string tenant")), EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout, "tenant");
    }

    [Theory]
    [InlineData("=> RateLimiterEndpointConventionBuilderExtensions.RequireRateLimiting<RouteGroupBuilder>(policyName: \"Customers\", builder: endpoints.MapGroup(\"/api/{customerId:long}\"));")]
    [InlineData("=> RateLimiterEndpointConventionBuilderExtensions.RequireRateLimiting<RouteGroupBuilder, string>(policy: Policy, builder: endpoints.MapGroup(\"/api/{customerId:long}\"));")]
    [InlineData("=> RateLimiterEndpointConventionBuilderExtensions.DisableRateLimiting<RouteGroupBuilder>(builder: endpoints.MapGroup(\"/api/{customerId:long}\"));")]
    [InlineData("=> Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions.RequireRateLimiting(builder: EndpointRouteBuilderExtensions.MapGroup(prefix: \"/api/{customerId:long}\", endpoints: endpoints), policy: Policy);")]
    public async Task Native_static_calls_support_explicit_generics_and_reordered_named_arguments(string mapBody)
    {
        AssertInfo(await Analyze(Source(mapBody, "[FromRoute] int customerId")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    [Theory]
    [InlineData("=> RateConventions.RequireRateLimiting<RouteGroupBuilder>(endpoints.MapGroup(\"/api/{customerId:long}\"), \"Customers\");")]
    [InlineData("=> RateConventions.RequireRateLimiting<RouteGroupBuilder, string>(endpoints.MapGroup(\"/api/{customerId:long}\"), Policy);")]
    [InlineData("=> RateConventions.DisableRateLimiting(endpoints.MapGroup(\"/api/{customerId:long}\"));")]
    public async Task Type_aliases_keep_native_method_identity(string mapBody)
    {
        var source = "using RateConventions = Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions;\n" + Source(mapBody, "[FromRoute] int customerId");
        AssertInfo(await Analyze(source), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    [Theory]
    [InlineData("=> endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy);")]
    [InlineData("{ return endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy); }")]
    [InlineData("{ var group = endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy); return group; }")]
    public async Task The_existing_three_Map_body_shapes_remain_readable(string mapBody)
    {
        AssertInfo(await Analyze(Source(mapBody, "[FromRoute] int customerId")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    [Fact]
    public async Task Nested_groups_interleaved_with_native_rate_and_existing_metadata_conventions_retain_both_prefixes()
    {
        const string body = "=> endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(\"Customers\").WithTags(\"Customers\").MapGroup(\"/sections/{sectionId:guid}\").RequireRateLimiting(Policy).DisableRateLimiting().WithDescription(\"Sections\");";
        AssertInfo(await Analyze(Source(body, "[FromRoute] int customerId, [FromRoute] Guid sectionId")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    [Theory]
    [InlineData(".RequireRateLimiting(PolicyName)")]
    [InlineData(".RequireRateLimiting(CreatePolicyName())")]
    [InlineData(".RequireRateLimiting(Policy)")]
    [InlineData(".RequireRateLimiting(CreatePolicy())")]
    public async Task Policy_argument_getters_and_factories_are_never_executed_by_prefix_analysis(string convention)
    {
        AssertInfo(await Analyze(Source("=> endpoints.MapGroup(\"/api/{customerId:long}\")" + convention + ";", "[FromRoute] int customerId")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    [Theory]
    [InlineData(".RequireRateLimiting(\"Customers\")")]
    [InlineData(".RequireRateLimiting(Policy)")]
    [InlineData(".DisableRateLimiting()")]
    public async Task Custom_extensions_with_native_rate_method_names_remain_unknown(string convention)
    {
        var source = Source("=> endpoints.MapGroup(\"/api/{customerId:long}\")" + convention + ";", "[FromRoute] int customerId") + """
                                                                                                                                    namespace Sample.Api
                                                                                                                                    {
                                                                                                                                        internal static class CustomGroups
                                                                                                                                        {
                                                                                                                                            public static RouteGroupBuilder RequireRateLimiting(this RouteGroupBuilder builder, string policyName) => builder.MapGroup("/rewritten/{other}");
                                                                                                                                            public static RouteGroupBuilder RequireRateLimiting(this RouteGroupBuilder builder, IRateLimiterPolicy<string> policy) => builder.MapGroup("/rewritten/{other}");
                                                                                                                                            public static RouteGroupBuilder DisableRateLimiting(this RouteGroupBuilder builder) => builder.MapGroup("/rewritten/{other}");
                                                                                                                                        }
                                                                                                                                    }
                                                                                                                                    """;
        Assert.Empty(await Analyze(source));
    }

    [Theory]
    [InlineData("=> CustomGroups.RequireRateLimiting<RouteGroupBuilder>(endpoints.MapGroup(\"/api/{customerId:long}\"), \"Customers\");")]
    [InlineData("=> CustomGroups.RequireRateLimiting<RouteGroupBuilder, string>(endpoints.MapGroup(\"/api/{customerId:long}\"), Policy);")]
    public async Task Custom_generic_receiver_returning_forwarders_do_not_gain_native_provenance(string mapBody)
    {
        var source = Source(mapBody, "[FromRoute] int customerId") + """
                                                                     namespace Sample.Api
                                                                     {
                                                                         internal static class CustomGroups
                                                                         {
                                                                             public static TBuilder RequireRateLimiting<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder => builder;
                                                                             public static TBuilder RequireRateLimiting<TBuilder, TKey>(this TBuilder builder, IRateLimiterPolicy<TKey> policy) where TBuilder : IEndpointConventionBuilder => builder;
                                                                         }
                                                                     }
                                                                     """;
        Assert.Empty(await Analyze(source));
    }

    [Theory]
    [InlineData("=> Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions.RequireRateLimiting(endpoints.MapGroup(\"/api/{customerId:long}\"), \"Customers\");")]
    [InlineData("=> Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions.RequireRateLimiting<RouteGroupBuilder, string>(endpoints.MapGroup(\"/api/{customerId:long}\"), Policy);")]
    [InlineData("=> Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions.DisableRateLimiting(endpoints.MapGroup(\"/api/{customerId:long}\"));")]
    public async Task Source_lookalikes_with_the_exact_native_containing_type_name_remain_unknown(string mapBody)
    {
        var source = Source(mapBody, "[FromRoute] int customerId") + """
                                                                     namespace Microsoft.AspNetCore.Builder
                                                                     {
                                                                         public static class RateLimiterEndpointConventionBuilderExtensions
                                                                         {
                                                                             public static TBuilder RequireRateLimiting<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder => builder;
                                                                             public static TBuilder RequireRateLimiting<TBuilder, TKey>(this TBuilder builder, IRateLimiterPolicy<TKey> policy) where TBuilder : IEndpointConventionBuilder => builder;
                                                                             public static TBuilder DisableRateLimiting<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder => builder;
                                                                         }
                                                                     }
                                                                     """;
        Assert.Empty(await Analyze(source));
    }

    [Fact]
    public async Task A_source_policy_interface_with_the_native_short_name_does_not_establish_native_identity()
    {
        var source = Source("=> endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy);", "[FromRoute] int customerId") + """
                                                                                                                                              namespace Sample.Api
                                                                                                                                              {
                                                                                                                                                  internal interface IRateLimiterPolicy<T> { }
                                                                                                                                                  internal static class CustomGroups
                                                                                                                                                  {
                                                                                                                                                      public static RouteGroupBuilder RequireRateLimiting<T>(this RouteGroupBuilder builder, IRateLimiterPolicy<T> policy) => builder;
                                                                                                                                                  }
                                                                                                                                              }
                                                                                                                                              """;
        Assert.Empty(await Analyze(source));
    }

    [Fact]
    public async Task Two_generic_parameters_on_an_old_convention_name_do_not_expand_its_native_contract()
    {
        var source = Source("=> CustomGroups.WithTags<RouteGroupBuilder, string>(endpoints.MapGroup(\"/api/{customerId:long}\"), Array.Empty<string>());", "[FromRoute] int customerId") + """
                                                                                                                                                                                           namespace Sample.Api
                                                                                                                                                                                           {
                                                                                                                                                                                               internal static class CustomGroups
                                                                                                                                                                                               {
                                                                                                                                                                                                   public static TBuilder WithTags<TBuilder, TTag>(this TBuilder builder, TTag[] tags) where TBuilder : IEndpointConventionBuilder => builder;
                                                                                                                                                                                               }
                                                                                                                                                                                           }
                                                                                                                                                                                           """;
        Assert.Empty(await Analyze(source));
    }

    [Theory]
    [InlineData("=> endpoints.MapGroup(Prefix).RequireRateLimiting(Policy);")]
    [InlineData("=> endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy).WithOrder(0);")]
    [InlineData("{ var group = endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy); group = group.MapGroup(\"/other\"); return group; }")]
    [InlineData("{ var group = endpoints.MapGroup(\"/api/{customerId:long}\").DisableRateLimiting(); _ = DateTime.UtcNow; return group; }")]
    [InlineData("=> DateTime.UtcNow.Year > 2000 ? endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy) : endpoints.MapGroup(\"/other\");")]
    [InlineData("=> Wrap(endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy));")]
    public async Task Rate_limiting_calls_do_not_expand_dynamic_prefixes_wrappers_or_statement_flow(string mapBody)
    {
        const string members = "private static string Prefix => throw new InvalidOperationException(); private static RouteGroupBuilder Wrap(RouteGroupBuilder group) => group;";
        Assert.Empty(await Analyze(Source(mapBody, "[FromRoute] int customerId", members)));
    }

    [Theory]
    [InlineData(".WithTags(\"Customers\").RequireAuthorization().RequireCors().AllowAnonymous()")]
    [InlineData(".WithName(\"Customers\").WithGroupName(\"Customers\").WithDisplayName(\"Customers\")")]
    public async Task Existing_one_parameter_native_conventions_retain_their_behavior(string conventions)
    {
        AssertInfo(await Analyze(Source("=> endpoints.MapGroup(\"/api/{customerId:long}\").RequireRateLimiting(Policy)" + conventions + ";", "[FromRoute] int customerId")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    private static Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> Analyze(string source) => NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);

    private static void AssertInfo(IEnumerable<Diagnostic> diagnostics, string id, string parameter)
    {
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0);
        Assert.Contains(parameter, diagnostic.GetMessage());
    }

    private static string Source(string mapBody, string parameters, string members = "")
    {
        return Imports + $$"""
                           namespace Sample.Api
                           {
                               internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
                               {
                                   {{PolicyMembers}}
                                   {{members}}
                                   public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) {{mapBody}}
                               }
                           }
                           namespace Sample.Api.Items
                           {
                               internal sealed class ItemEndpoint : IEndpoint
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   [StringSyntax("Route")]
                                   public string Pattern => "/items";
                                   public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                               }
                           }
                           """;
    }
}