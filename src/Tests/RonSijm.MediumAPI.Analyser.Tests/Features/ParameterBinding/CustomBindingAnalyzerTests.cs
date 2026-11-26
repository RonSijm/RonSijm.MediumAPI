using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

// Semantic regressions use real framework contract symbols without executing custom binders or parsers.
public sealed class CustomBindingAnalyzerTests
{
    private const string Imports = """
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Reflection;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.Routing;
                                   using Microsoft.Extensions.DependencyInjection;
                                   using RonSijm.MediumAPI;

                                   """;

    private const string StructParser = """
                                        internal readonly struct Parsed
                                        {
                                            public static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException("Analysis must not execute parsers.");
                                        }
                                        """;

    [Theory]
    [InlineData("public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound?> BindAsync(HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound?> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();")]
    [InlineData("public static async ValueTask<Bound?> BindAsync(HttpContext context)\n{\n    await Task.Yield();\n    throw new InvalidOperationException();\n}")]
    public async Task Known_struct_context_binders_do_not_require_a_route_token(string method)
    {
        NoDiagnostics(await Analyze(Source("Bound value", "internal readonly struct Bound { " + method + " }")));
    }

    [Fact]
    public async Task Nullable_input_uses_the_underlying_struct_binder()
    {
        NoDiagnostics(await Analyze(Source("Bound? value", """
                                                           internal readonly struct Bound
                                                           {
                                                               public static ValueTask<Bound?> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                           }
                                                           """)));
    }

    [Fact]
    public async Task Explicit_native_bindable_interface_is_a_context_binding_contract()
    {
        NoDiagnostics(await Analyze(Source("Bound value", """
                                                          internal sealed class Bound : IBindableFromHttpContext<Bound>
                                                          {
                                                              static ValueTask<Bound?> IBindableFromHttpContext<Bound>.BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();
                                                          }
                                                          """)));
    }

    [Fact]
    public async Task Opaque_context_binding_can_consume_route_tokens_without_matching_a_parameter_name()
    {
        NoDiagnostics(await Analyze(Source("Bound selection", """
                                                              internal readonly struct Bound
                                                              {
                                                                  public static ValueTask<Bound> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException("The binder may read any route value.");
                                                              }
                                                              """, pattern: "/items/{tenant}/{itemId:long}")));
    }

    [Fact]
    public async Task Opaque_binder_does_not_hide_an_ordinary_parameter_constraint_mismatch()
    {
        var diagnostics = await Analyze(Source("Bound selection, int itemId", """
                                                                              internal readonly struct Bound
                                                                              {
                                                                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                              }
                                                                              """, pattern: "/items/{itemId:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Fact]
    public async Task BindAsync_precedes_string_parsing_for_implicit_binding()
    {
        NoDiagnostics(await Analyze(Source("Bound selection", """
                                                              internal readonly struct Bound
                                                              {
                                                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                  public static bool TryParse(string text, out Bound result) => throw new InvalidOperationException();
                                                              }
                                                              """, pattern: "/items/{itemId:long}")));
    }

    [Theory]
    [InlineData("FromQuery")]
    [InlineData("FromServices")]
    [InlineData("FromKeyedServices(\"fixture\")")]
    public async Task Explicit_non_route_binding_does_not_gain_opaque_route_suppression(string binding)
    {
        var diagnostics = await Analyze(Source("[" + binding + "] Bound selection", """
                                                                                    internal readonly struct Bound
                                                                                    {
                                                                                        public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                                        public static bool TryParse(string text, out Bound result) => throw new InvalidOperationException();
                                                                                    }
                                                                                    """, pattern: "/items/{itemId:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Fact]
    public async Task Explicit_route_binding_does_not_treat_BindAsync_as_a_string_parser()
    {
        var diagnostics = await Analyze(Source("[FromRoute] Bound value", """
                                                                          internal readonly struct Bound
                                                                          {
                                                                              public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                          }
                                                                          """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Explicit_route_binding_can_use_the_same_types_separate_string_parser()
    {
        NoDiagnostics(await Analyze(Source("[FromRoute] Bound value", """
                                                                      internal readonly struct Bound
                                                                      {
                                                                          public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                          public static bool TryParse(string text, out Bound result) => throw new InvalidOperationException();
                                                                      }
                                                                      """, pattern: "/items/{value:long}")));
    }

    [Theory]
    [InlineData("private static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound> BindAsync<T>(HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public static Task<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<int> BindAsync(HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound> BindAsync(HttpRequest request) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound> BindAsync(ref HttpContext context) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound> BindAsync(HttpContext context, string parameter) => throw new InvalidOperationException();")]
    [InlineData("public static ValueTask<Bound> BindAsync(HttpContext context, __arglist) => throw new InvalidOperationException();")]
    public async Task Unknown_struct_binder_shapes_do_not_suppress_the_existing_missing_route_rule(string method)
    {
        var diagnostics = await Analyze(Source("Bound value", "internal struct Bound { " + method + " }"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Ref_returning_context_binder_is_not_a_known_value_binder()
    {
        var diagnostics = await Analyze(Source("Bound value", """
                                                              internal struct Bound
                                                              {
                                                                  private static ValueTask<Bound> pending;
                                                                  public static ref ValueTask<Bound> BindAsync(HttpContext context) => ref pending;
                                                              }
                                                              """));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Invalid_preferred_two_argument_binder_does_not_fall_back_to_a_one_argument_guess()
    {
        var diagnostics = await Analyze(Source("Bound value", """
                                                              internal readonly struct Bound
                                                              {
                                                                  public static ValueTask<int> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();
                                                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                              }
                                                              """));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Lookalike_HttpContext_and_ValueTask_types_are_not_native_binder_contracts()
    {
        var diagnostics = await Analyze(Source("Bound value", """
                                                              internal readonly struct Bound
                                                              {
                                                                  public static ValueTask<Bound> BindAsync(Lookalikes.HttpContext context) => throw new InvalidOperationException();
                                                              }
                                                              namespace Lookalikes { internal sealed class HttpContext { } }
                                                              """));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);

        var fakeTask = await Analyze(Source("Bound value", """
                                                           internal readonly struct Bound
                                                           {
                                                               public static Lookalikes.ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                           }
                                                           namespace Lookalikes { internal readonly struct ValueTask<T> { } }
                                                           """));
        AssertDiagnostic(fakeTask, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task A_lookalike_bindable_interface_does_not_supply_native_binding()
    {
        var diagnostics = await Analyze(Source("Bound value", """
                                                              internal readonly struct Bound : Lookalikes.IBindableFromHttpContext<Bound> { }
                                                              namespace Lookalikes { internal interface IBindableFromHttpContext<T> { } }
                                                              """));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Inherited_class_context_binder_can_consume_unmatched_route_tokens()
    {
        NoDiagnostics(await Analyze(Source("Bound selection", """
                                                              internal abstract class BinderBase
                                                              {
                                                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                              }
                                                              internal sealed class Bound : BinderBase { }
                                                              """, pattern: "/items/{itemId}")));
    }

    [Theory]
    [InlineData("public static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();")]
    [InlineData("public static bool TryParse(string text, IFormatProvider provider, out Parsed parsed) => throw new InvalidOperationException();")]
    public async Task A_declared_class_parser_participates_in_route_matching(string method)
    {
        NoDiagnostics(await Analyze(Source("Parsed value", "internal sealed class Parsed { " + method + " }", pattern: "/items/{value}")));
    }

    [Theory]
    [InlineData("Parsed")]
    [InlineData("Parsed?")]
    public async Task Custom_value_parser_is_not_compared_to_the_primitive_constraint_clr_type(string parameterType)
    {
        NoDiagnostics(await Analyze(Source("[FromRoute] " + parameterType + " value", StructParser, pattern: "/items/{value:long}")));
    }

    [Fact]
    public async Task A_class_parser_and_explicit_route_alias_keep_the_local_token_binding()
    {
        NoDiagnostics(await Analyze(Source("[FromRoute(Name = \"value\")] Parsed selected", """
                                                                                            internal sealed class Parsed
                                                                                            {
                                                                                                public static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();
                                                                                            }
                                                                                            """, pattern: "/items/{value:long}")));
    }

    [Fact]
    public async Task Explicit_native_IParsable_implementation_is_a_string_parser()
    {
        NoDiagnostics(await Analyze(Source("Parsed value", """
                                                           internal sealed class Parsed : IParsable<Parsed>
                                                           {
                                                               static Parsed IParsable<Parsed>.Parse(string text, IFormatProvider provider) => throw new InvalidOperationException();
                                                               static bool IParsable<Parsed>.TryParse(string text, IFormatProvider provider, out Parsed parsed) => throw new InvalidOperationException();
                                                           }
                                                           """, pattern: "/items/{value:long}")));
    }

    [Fact]
    public async Task Native_Uri_remains_a_string_bound_route_candidate()
    {
        NoDiagnostics(await Analyze(Source("Uri value", "", pattern: "/items/{value}")));
    }

    [Fact]
    public async Task Inherited_constraint_does_not_impose_a_primitive_clr_type_on_a_custom_parser()
    {
        NoDiagnostics(await Analyze(Source("[FromRoute(Name = \"value\")] Parsed selected", StructParser, groupPrefix: "/api/{value:long}")));
    }

    [Fact]
    public async Task Parsable_parameters_outside_the_route_keep_the_existing_explicit_query_convention()
    {
        var diagnostics = await Analyze(Source("Parsed value", """
                                                               internal sealed class Parsed
                                                               {
                                                                   public static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();
                                                               }
                                                               """));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);

        NoDiagnostics(await Analyze(Source("[FromQuery] Parsed value", StructParser)));
    }

    [Theory]
    [InlineData("private static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();")]
    [InlineData("public bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();")]
    [InlineData("public static bool TryParse<T>(string text, out Parsed parsed) => throw new InvalidOperationException();")]
    [InlineData("public static int TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();")]
    [InlineData("public static bool TryParse(string text, out long parsed) => throw new InvalidOperationException();")]
    [InlineData("public static bool TryParse(string text, ref Parsed parsed) => throw new InvalidOperationException();")]
    [InlineData("public static bool TryParse(object text, out Parsed parsed) => throw new InvalidOperationException();")]
    [InlineData("public static bool TryParse(string text, out Parsed parsed, __arglist) => throw new InvalidOperationException();")]
    public async Task Unknown_parser_shapes_do_not_receive_a_custom_constraint_exemption(string method)
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", "internal sealed class Parsed { " + method + " }", pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Ambiguous_declared_parser_candidates_remain_unknown()
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", """
                                                                           internal sealed class Parsed
                                                                           {
                                                                               public static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();
                                                                               public static bool TryParse<T>(string text, out Parsed parsed) => throw new InvalidOperationException();
                                                                           }
                                                                           """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Ref_returning_parser_does_not_receive_a_string_parser_exemption()
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", """
                                                                           internal sealed class Parsed
                                                                           {
                                                                               private static bool success;
                                                                               public static ref bool TryParse(string text, out Parsed parsed)
                                                                               {
                                                                                   parsed = null!;
                                                                                   return ref success;
                                                                               }
                                                                           }
                                                                           """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Inherited_class_parser_matches_the_derived_parameter_type()
    {
        NoDiagnostics(await Analyze(Source("[FromRoute] Parsed value", """
                                                                       internal abstract class ParserBase
                                                                       {
                                                                           public static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();
                                                                       }
                                                                       internal sealed class Parsed : ParserBase { }
                                                                       """, pattern: "/items/{value:long}")));
    }

    [Fact]
    public async Task Concrete_interface_parser_helpers_remain_outside_the_proven_subset()
    {
        var interfaceHelper = await Analyze(Source("[FromRoute] Parsed value", """
                                                                               internal interface IParserHelper
                                                                               {
                                                                                   public static bool TryParse(string text, out Parsed parsed) => throw new InvalidOperationException();
                                                                               }
                                                                               internal sealed class Parsed : IParserHelper { }
                                                                               """, pattern: "/items/{value:long}"));
        AssertDiagnostic(interfaceHelper, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task A_lookalike_IParsable_interface_is_not_the_native_self_parser_contract()
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", """
                                                                           internal sealed class Parsed : Lookalikes.IParsable<Parsed> { }
                                                                           namespace Lookalikes { internal interface IParsable<T> { } }
                                                                           """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("int", "long")]
    [InlineData("long", "int")]
    [InlineData("Guid", "long")]
    [InlineData("DateTime", "long")]
    [InlineData("string", "int")]
    public async Task Built_in_primitive_constraint_checks_remain_errors(string parameterType, string constraint)
    {
        var diagnostics = await Analyze(Source("[FromRoute] " + parameterType + " value", "", pattern: "/items/{value:" + constraint + "}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Ordinary_inherited_primitive_mismatch_remains_an_information_advisory()
    {
        var diagnostics = await Analyze(Source("[FromRoute] int value", "", groupPrefix: "/api/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task The_existing_Get_invocation_branch_recognizes_a_class_parser()
    {
        var source = Imports + """
                               internal sealed class Parsed
                               {
                                   public static bool TryParse(string text, out Parsed result) => throw new InvalidOperationException();
                               }
                               internal static class Invocations
                               {
                                   public static void Configure() => Get("/items/{value:long}", static (Parsed value) => value);
                                   private static void Get(string pattern, Func<Parsed, Parsed> handler)
                                   {
                                   }
                               }
                               """;
        NoDiagnostics(await Analyze(source));
    }

    [Fact]
    public async Task The_existing_Get_invocation_branch_allows_opaque_context_binding()
    {
        var source = Imports + """
                               internal readonly struct Bound
                               {
                                   public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                               }
                               internal static class Invocations
                               {
                                   public static void Configure() => Get("/items/{tenant}/{itemId:long}", static (Bound selection) => selection);
                                   private static void Get(string pattern, Func<Bound, Bound> handler)
                                   {
                                   }
                               }
                               """;
        NoDiagnostics(await Analyze(source));
    }

    [Fact]
    public async Task Custom_explicit_source_metadata_blocks_context_opacity_without_reading_its_getter()
    {
        var diagnostics = await Analyze(Source("[QuerySlot] Bound selection", """
                                                                              internal readonly struct Bound
                                                                              {
                                                                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                              }
                                                                              [AttributeUsage(AttributeTargets.Parameter)]
                                                                              internal sealed class QuerySlotAttribute : Attribute, Microsoft.AspNetCore.Http.Metadata.IFromQueryMetadata
                                                                              {
                                                                                  public string? Name => throw new InvalidOperationException("Analysis must not execute metadata getters.");
                                                                              }
                                                                              """, pattern: "/items/{itemId:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Theory]
    [InlineData("namespace Microsoft.AspNetCore.Http { internal sealed class HttpContext { } }")]
    [InlineData("namespace System.Threading.Tasks { internal readonly struct ValueTask<T> { } }")]
    public async Task Source_types_with_native_full_names_do_not_prove_a_framework_binder(string lookalike)
    {
        var source = Source("Bound value", """
                                           internal readonly struct Bound
                                           {
                                               public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                           }
                                           """) + lookalike;
        var diagnostics = await Analyze(source);
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task A_source_interface_with_the_native_full_name_does_not_prove_string_parsing()
    {
        var source = Source("[FromRoute] Parsed value", "internal sealed class Parsed : IParsable<Parsed> { }", pattern: "/items/{value:long}")
                     + "namespace System { internal interface IParsable<T> { } }";
        var diagnostics = await Analyze(source);
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("HttpContext context")]
    [InlineData("HttpContext context, ParameterInfo parameter")]
    public async Task Constructed_generic_class_bases_substitute_the_context_binder_result(string parameters)
    {
        NoDiagnostics(await Analyze(Source("Bound selection", $$"""
                                                                internal abstract class BinderBase<T>
                                                                {
                                                                    public static ValueTask<T> BindAsync({{parameters}}) => throw new InvalidOperationException();
                                                                }
                                                                internal abstract class Intermediate<T> : BinderBase<T> { }
                                                                internal sealed class Bound : Intermediate<Bound> { }
                                                                """, pattern: "/items/{itemId}")));
    }

    [Theory]
    [InlineData("string text")]
    [InlineData("string text, IFormatProvider provider")]
    public async Task Constructed_generic_class_bases_substitute_the_parser_out_type(string parameters)
    {
        NoDiagnostics(await Analyze(Source("[FromRoute] Parsed value", $$"""
                                                                         internal abstract class ParserBase<T>
                                                                         {
                                                                             public static bool TryParse({{parameters}}, out T result) => throw new InvalidOperationException();
                                                                         }
                                                                         internal abstract class Intermediate<T> : ParserBase<T> { }
                                                                         internal sealed class Parsed : Intermediate<Parsed> { }
                                                                         """, pattern: "/items/{value:long}")));
    }

    [Fact]
    public async Task Inherited_two_argument_binder_is_preferred_to_a_malformed_declared_fallback()
    {
        NoDiagnostics(await Analyze(Source("Bound selection", """
                                                              internal abstract class BinderBase
                                                              {
                                                                  public static ValueTask<Bound> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();
                                                              }
                                                              internal sealed class Bound : BinderBase
                                                              {
                                                                  public static Task<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                              }
                                                              """, pattern: "/items/{itemId}")));
    }

    [Fact]
    public async Task Inherited_malformed_preferred_binder_is_not_replaced_by_a_declared_fallback_guess()
    {
        var diagnostics = await Analyze(Source("Bound selection", """
                                                                  internal abstract class BinderBase
                                                                  {
                                                                      public static ValueTask<int> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();
                                                                  }
                                                                  internal sealed class Bound : BinderBase
                                                                  {
                                                                      public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                  }
                                                                  """, pattern: "/items/{itemId}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Inherited_provider_parser_is_preferred_to_a_malformed_declared_fallback()
    {
        NoDiagnostics(await Analyze(Source("[FromRoute] Parsed value", """
                                                                       internal abstract class ParserBase
                                                                       {
                                                                           public static bool TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();
                                                                       }
                                                                       internal sealed class Parsed : ParserBase
                                                                       {
                                                                           public static int TryParse(string text, out Parsed result) => throw new InvalidOperationException();
                                                                       }
                                                                       """, pattern: "/items/{value:long}")));
    }

    [Fact]
    public async Task Malformed_inherited_preferred_parser_remains_conservative_despite_a_valid_fallback()
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", """
                                                                           internal abstract class ParserBase
                                                                           {
                                                                               public static int TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();
                                                                           }
                                                                           internal sealed class Parsed : ParserBase
                                                                           {
                                                                               public static bool TryParse(string text, out Parsed result) => throw new InvalidOperationException();
                                                                           }
                                                                           """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("ValueTask<int>", "ValueTask<Bound>", true)]
    [InlineData("ValueTask<Bound>", "ValueTask<Bound>", true)]
    [InlineData("ValueTask<Bound>", "ValueTask<int>", false)]
    public async Task The_nearest_matching_binder_declaration_hides_the_base_signature(string baseResult, string derivedResult, bool recognized)
    {
        var diagnostics = await Analyze(Source("Bound selection", $$"""
                                                                    internal abstract class BinderBase
                                                                    {
                                                                        public static {{baseResult}} BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();
                                                                    }
                                                                    internal sealed class Bound : BinderBase
                                                                    {
                                                                        public new static {{derivedResult}} BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();
                                                                    }
                                                                    """, pattern: "/items/{itemId}"));
        if (recognized)
        {
            NoDiagnostics(diagnostics);
        }
        else
        {
            AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
        }
    }

    [Theory]
    [InlineData("int", "bool", true)]
    [InlineData("bool", "bool", true)]
    [InlineData("bool", "int", false)]
    public async Task The_nearest_matching_parser_declaration_hides_the_base_signature(string baseResult, string derivedResult, bool recognized)
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", $$"""
                                                                             internal abstract class ParserBase
                                                                             {
                                                                                 public static {{baseResult}} TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();
                                                                             }
                                                                             internal sealed class Parsed : ParserBase
                                                                             {
                                                                                 public new static {{derivedResult}} TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();
                                                                             }
                                                                             """, pattern: "/items/{value:long}"));
        if (recognized)
        {
            NoDiagnostics(diagnostics);
        }
        else
        {
            AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
        }
    }

    [Theory]
    [InlineData("private static ValueTask<int> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();", "HttpContext context")]
    [InlineData("public ValueTask<int> BindAsync(HttpContext context, ParameterInfo parameter) => throw new InvalidOperationException();", "HttpContext context")]
    [InlineData("public static ValueTask<int> BindAsync(HttpRequest request, ParameterInfo parameter) => throw new InvalidOperationException();", "HttpContext context, ParameterInfo parameter")]
    public async Task Unrelated_or_non_public_static_base_methods_do_not_poison_a_known_exact_binder(string baseMethod, string parameters)
    {
        NoDiagnostics(await Analyze(Source("Bound selection", $$"""
                                                                internal abstract class BinderBase { {{baseMethod}} }
                                                                internal sealed class Bound : BinderBase
                                                                {
                                                                    public static ValueTask<Bound> BindAsync({{parameters}}) => throw new InvalidOperationException();
                                                                }
                                                                """, pattern: "/items/{itemId}")));
    }

    [Theory]
    [InlineData("private static int TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();", "string text")]
    [InlineData("public int TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();", "string text")]
    [InlineData("public static int TryParse(object text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();", "string text, IFormatProvider provider")]
    public async Task Unrelated_or_non_public_static_base_methods_do_not_poison_a_known_exact_parser(string baseMethod, string parameters)
    {
        NoDiagnostics(await Analyze(Source("[FromRoute] Parsed value", $$"""
                                                                         internal abstract class ParserBase { {{baseMethod}} }
                                                                         internal sealed class Parsed : ParserBase
                                                                         {
                                                                             public static bool TryParse({{parameters}}, out Parsed result) => throw new InvalidOperationException();
                                                                         }
                                                                         """, pattern: "/items/{value:long}")));
    }

    [Theory]
    [InlineData("object context, ParameterInfo parameter")]
    [InlineData("HttpContext context, ICustomAttributeProvider parameter")]
    public async Task Inherited_assignable_preferred_binder_candidates_block_a_fallback_guess(string parameters)
    {
        var diagnostics = await Analyze(Source("Bound selection", $$"""
                                                                    internal abstract class BinderBase
                                                                    {
                                                                        public static ValueTask<int> BindAsync({{parameters}}) => throw new InvalidOperationException();
                                                                    }
                                                                    internal sealed class Bound : BinderBase
                                                                    {
                                                                        public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                    }
                                                                    """, pattern: "/items/{itemId}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData(false, "HttpRequest context, ParameterInfo parameter")]
    [InlineData(true, "HttpRequest context, ParameterInfo parameter")]
    [InlineData(false, "string context, ParameterInfo parameter")]
    [InlineData(true, "string context, ParameterInfo parameter")]
    [InlineData(false, "HttpContext context, MemberInfo parameter")]
    [InlineData(true, "HttpContext context, MemberInfo parameter")]
    public async Task Unrelated_preferred_arity_overloads_do_not_block_an_exact_context_binder_fallback(bool inherited, string parameters)
    {
        var unrelated = "public static ValueTask<int> BindAsync(" + parameters + ") => throw new InvalidOperationException();";
        NoDiagnostics(await Analyze(Source("Bound selection", $$"""
                                                                internal abstract class BinderBase
                                                                {
                                                                    {{(inherited ? unrelated : "")}}
                                                                }
                                                                internal sealed class Bound : BinderBase
                                                                {
                                                                    {{(inherited ? "" : unrelated)}}
                                                                    public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                }
                                                                """, pattern: "/items/{itemId}")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unrelated_preferred_arity_overloads_do_not_block_an_exact_parser_fallback(bool inherited)
    {
        const string unrelated = "public static bool TryParse(int text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();";
        NoDiagnostics(await Analyze(Source("[FromRoute] Parsed value", $$"""
                                                                         internal abstract class ParserBase
                                                                         {
                                                                             {{(inherited ? unrelated : "")}}
                                                                         }
                                                                         internal sealed class Parsed : ParserBase
                                                                         {
                                                                             {{(inherited ? "" : unrelated)}}
                                                                             public static bool TryParse(string text, out Parsed result) => throw new InvalidOperationException();
                                                                         }
                                                                         """, pattern: "/items/{value:long}")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_assignable_preferred_parser_out_type_blocks_a_lower_arity_guess(bool inherited)
    {
        const string preferred = "public static bool TryParse(string text, IFormatProvider provider, out ParserBase result) => throw new InvalidOperationException();";
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", $$"""
                                                                             internal abstract class ParserBase
                                                                             {
                                                                                 {{(inherited ? preferred : "")}}
                                                                             }
                                                                             internal sealed class Parsed : ParserBase
                                                                             {
                                                                                 {{(inherited ? "" : preferred)}}
                                                                                 public static bool TryParse(string text, out Parsed result) => throw new InvalidOperationException();
                                                                             }
                                                                             """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task A_derived_ref_parser_hides_the_base_out_signature_without_proving_string_binding()
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", """
                                                                           internal abstract class ParserBase
                                                                           {
                                                                               public static bool TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();
                                                                           }
                                                                           internal sealed class Parsed : ParserBase
                                                                           {
                                                                               public new static bool TryParse(string text, IFormatProvider provider, ref Parsed result) => throw new InvalidOperationException();
                                                                           }
                                                                           """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();")]
    public async Task Generic_or_ambiguous_inherited_binder_candidates_remain_unknown(string otherMethod)
    {
        var diagnostics = await Analyze(Source("Bound selection", $$"""
                                                                    internal abstract class BinderBase
                                                                    {
                                                                        public static ValueTask<Bound> BindAsync<T>(HttpContext context) => throw new InvalidOperationException();
                                                                        {{otherMethod}}
                                                                    }
                                                                    internal sealed class Bound : BinderBase { }
                                                                    """, pattern: "/items/{itemId}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("public static bool TryParse(string text, out Parsed result) => throw new InvalidOperationException();")]
    public async Task Generic_or_ambiguous_inherited_parser_candidates_remain_unknown(string otherMethod)
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", $$"""
                                                                             internal abstract class ParserBase
                                                                             {
                                                                                 public static bool TryParse<T>(string text, out Parsed result) => throw new InvalidOperationException();
                                                                                 {{otherMethod}}
                                                                             }
                                                                             internal sealed class Parsed : ParserBase { }
                                                                             """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task An_inherited_binder_returning_the_base_type_does_not_bind_the_derived_type()
    {
        var diagnostics = await Analyze(Source("Bound selection", """
                                                                  internal abstract class BinderBase
                                                                  {
                                                                      public static ValueTask<BinderBase> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                  }
                                                                  internal sealed class Bound : BinderBase { }
                                                                  """, pattern: "/items/{itemId}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task An_inherited_parser_out_parameter_must_be_the_derived_parameter_type()
    {
        var diagnostics = await Analyze(Source("[FromRoute] Parsed value", """
                                                                           internal abstract class ParserBase
                                                                           {
                                                                               public static bool TryParse(string text, out ParserBase result) => throw new InvalidOperationException();
                                                                           }
                                                                           internal sealed class Parsed : ParserBase { }
                                                                           """, pattern: "/items/{value:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Explicit_source_metadata_keeps_precedence_over_an_inherited_context_binder()
    {
        var diagnostics = await Analyze(Source("[FromQuery] Bound selection", """
                                                                              internal abstract class BinderBase
                                                                              {
                                                                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                              }
                                                                              internal sealed class Bound : BinderBase { }
                                                                              """, pattern: "/items/{itemId}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Inherited_parser_preserves_route_alias_and_namespace_constraint_binding()
    {
        NoDiagnostics(await Analyze(Source("[FromRoute(Name = \"value\")] Parsed selected", """
                                                                                            internal abstract class ParserBase
                                                                                            {
                                                                                                public static bool TryParse(string text, IFormatProvider provider, out Parsed result) => throw new InvalidOperationException();
                                                                                            }
                                                                                            internal sealed class Parsed : ParserBase { }
                                                                                            """, groupPrefix: "/api/{value:long}")));
    }

    [Fact]
    public async Task Inherited_concrete_interface_binder_helpers_are_still_not_guessed()
    {
        var diagnostics = await Analyze(Source("Bound selection", """
                                                                  internal interface IBinderHelper
                                                                  {
                                                                      public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException();
                                                                  }
                                                                  internal abstract class BinderBase : IBinderHelper { }
                                                                  internal sealed class Bound : BinderBase { }
                                                                  """, pattern: "/items/{itemId}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string source) => NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);

    private static void NoDiagnostics(IEnumerable<Diagnostic> diagnostics) => Assert.Empty(diagnostics);

    private static void AssertDiagnostic(IEnumerable<Diagnostic> diagnostics, string id, DiagnosticSeverity severity)
    {
        var diagnostic = Assert.Single(diagnostics, candidate => candidate.Id == id);
        Assert.Equal(severity, diagnostic.Severity);
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0);
    }

    private static string Source(string parameters, string types, string pattern = "/items", string groupPrefix = "/api")
    {
        return Imports + $$"""
                           namespace Sample.Api
                           {
                               internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
                               {
                                   public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("{{groupPrefix}}");
                               }
                           }
                           namespace Sample.Api.Items
                           {
                               internal sealed class BindingEndpoint : IEndpoint
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   [StringSyntax("Route")]
                                   public string Pattern => "{{pattern}}";
                                   public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                               }
                               {{types}}
                           }
                           """;
    }
}