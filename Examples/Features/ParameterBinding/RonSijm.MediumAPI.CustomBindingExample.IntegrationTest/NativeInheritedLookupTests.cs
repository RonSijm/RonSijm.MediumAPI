using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RonSijm.MediumAPI.CustomBindingExample.IntegrationTest;

// Native characterization, independent of MediumAPI registration. These cases keep
// unsupported or rejected contracts away from the runnable example's discovery.
public sealed class NativeInheritedLookupTests
{
    [Theory]
    [InlineData("value-task-base")]
    [InlineData("task-base")]
    [InlineData("parser-base")]
    [InlineData("invalid-preferred-task")]
    [InlineData("invalid-preferred-parser")]
    [InlineData("class-before-interfaces")]
    public async Task Native_lookup_uses_the_selected_valid_method(string scenario)
    {
        Delegate handler = scenario switch
        {
            "value-task-base" => (Func<ValidOverValueTaskRequest, IResult>)(value => TypedResults.Json(value.Source)),
            "task-base" => (Func<ValidOverTaskRequest, IResult>)(value => TypedResults.Json(value.Source)),
            "parser-base" => (Func<ValidOverIntParser, IResult>)(value => TypedResults.Json(value.Source)),
            "invalid-preferred-task" => (Func<TaskPreferredFallbackRequest, IResult>)(value => TypedResults.Json(value.Source)),
            "invalid-preferred-parser" => (Func<IntPreferredFallbackParser, IResult>)(value => TypedResults.Json(value.Source)),
            "class-before-interfaces" => (Func<ClassMethodWinsRequest, IResult>)(value => TypedResults.Json(value.Source)),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        await using var app = CreateNative(handler);
        await app.StartAsync();
        using var client = app.GetTestClient();

        Assert.Equal(scenario is "invalid-preferred-task" or "invalid-preferred-parser" ? "fallback" : "derived", await client.GetFromJsonAsync<string>("/native/abc"));
    }

    [Theory]
    [InlineData("invalid-derived-binder", "BindAsync")]
    [InlineData("invalid-derived-parser", "TryParse")]
    [InlineData("wrong-preferred-result", "BindAsync")]
    [InlineData("assignable-preferred-result", "BindAsync")]
    [InlineData("ambiguous-interfaces", "ambiguity")]
    public void Native_composition_rejects_invalid_selected_or_ambiguous_methods(string scenario, string expectedMessage)
    {
        Delegate handler = scenario switch
        {
            "invalid-derived-binder" => (Func<InvalidDerivedBinder, IResult>)(value => TypedResults.Ok()),
            "invalid-derived-parser" => (Func<InvalidDerivedParser, IResult>)(value => TypedResults.Ok()),
            "wrong-preferred-result" => (Func<WrongPreferredResult, IResult>)(value => TypedResults.Ok()),
            "assignable-preferred-result" => (Func<AssignablePreferredResult, IResult>)(value => TypedResults.Ok()),
            "ambiguous-interfaces" => (Func<AmbiguousInterfaceRequest, IResult>)(value => TypedResults.Ok()),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();

        var failure = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(handler, new RequestDelegateFactoryOptions { ServiceProvider = services, RouteParameterNames = ["value"] }));

        Assert.Contains(expectedMessage, failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Native_assignable_base_out_parser_composes_and_false_stays_a_binding_failure()
    {
        var handlerCalled = false;
        Func<WrongParserOut, IResult> handler = value =>
        {
            handlerCalled = true;
            return TypedResults.Ok();
        };
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var result = RequestDelegateFactory.Create(handler, new RequestDelegateFactoryOptions { ServiceProvider = services, RouteParameterNames = ["value"] });
        Assert.DoesNotContain(result.EndpointMetadata, metadata => metadata is IAcceptsMetadata);

        // Reflection's DefaultBinder can select out Base for requested out Derived.
        // This wider native shape remains outside the analyzer's exact self-out rule.
        await using var app = CreateNative(handler);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/native/abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(handlerCalled);
    }

    private static WebApplication CreateNative(Delegate handler)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        var app = builder.Build();
        app.MapGet("/native/{value}", handler);
        return app;
    }

    public abstract class InvalidValueTaskBase
    {
        public static ValueTask<int> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult(17);
    }

    public sealed class ValidOverValueTaskRequest : InvalidValueTaskBase
    {
        public string Source => "derived";
        public new static ValueTask<ValidOverValueTaskRequest?> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult<ValidOverValueTaskRequest?>(new());
    }

    public abstract class InvalidTaskBase
    {
        public static Task<ValidOverTaskRequest?> BindAsync(HttpContext context, ParameterInfo parameter) => Task.FromResult<ValidOverTaskRequest?>(null);
    }

    public sealed class ValidOverTaskRequest : InvalidTaskBase
    {
        public string Source => "derived";
        public new static ValueTask<ValidOverTaskRequest?> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult<ValidOverTaskRequest?>(new());
    }

    public abstract class InvalidIntParserBase
    {
        public static int TryParse(string? text, IFormatProvider? provider, out ValidOverIntParser result)
        {
            result = null!;
            return 0;
        }
    }

    public sealed class ValidOverIntParser : InvalidIntParserBase
    {
        public string Source => "derived";

        public new static bool TryParse(string? text, IFormatProvider? provider, out ValidOverIntParser result)
        {
            result = new();
            return true;
        }
    }

    public abstract class ValidBinderBase
    {
        public static ValueTask<InvalidDerivedBinder?> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult<InvalidDerivedBinder?>(new());
    }

    public sealed class InvalidDerivedBinder : ValidBinderBase
    {
        public new static ValueTask<int> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult(17);
    }

    public abstract class ValidParserBase
    {
        public static bool TryParse(string? text, IFormatProvider? provider, out InvalidDerivedParser result)
        {
            result = new();
            return true;
        }
    }

    public sealed class InvalidDerivedParser : ValidParserBase
    {
        public new static int TryParse(string? text, IFormatProvider? provider, out InvalidDerivedParser result)
        {
            result = new();
            return 17;
        }
    }

    public abstract class WrongPreferredBase
    {
        public static ValueTask<int> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult(17);
    }

    public sealed class WrongPreferredResult : WrongPreferredBase
    {
        public static ValueTask<WrongPreferredResult?> BindAsync(HttpContext context) => ValueTask.FromResult<WrongPreferredResult?>(new());
    }

    public abstract class AssignablePreferredBase
    {
        public static ValueTask<int> BindAsync(object context, ParameterInfo parameter) => ValueTask.FromResult(17);
    }

    public sealed class AssignablePreferredResult : AssignablePreferredBase
    {
        public static ValueTask<AssignablePreferredResult?> BindAsync(HttpContext context) => ValueTask.FromResult<AssignablePreferredResult?>(new());
    }

    public abstract class TaskPreferredBase
    {
        public static Task<TaskPreferredFallbackRequest?> BindAsync(HttpContext context, ParameterInfo parameter) => Task.FromResult<TaskPreferredFallbackRequest?>(null);
    }

    public sealed class TaskPreferredFallbackRequest : TaskPreferredBase
    {
        public string Source => "fallback";
        public static ValueTask<TaskPreferredFallbackRequest?> BindAsync(HttpContext context) => ValueTask.FromResult<TaskPreferredFallbackRequest?>(new());
    }

    public abstract class IntPreferredParserBase
    {
        public static int TryParse(string? text, IFormatProvider? provider, out IntPreferredFallbackParser result)
        {
            result = null!;
            return 0;
        }
    }

    public sealed class IntPreferredFallbackParser : IntPreferredParserBase
    {
        public string Source => "fallback";

        public static bool TryParse(string? text, out IntPreferredFallbackParser result)
        {
            result = new();
            return true;
        }
    }

    public abstract class WrongParserOutBase
    {
        public static bool TryParse(string? text, IFormatProvider? provider, out WrongParserOutBase result)
        {
            result = null!;
            return false;
        }
    }

    public sealed class WrongParserOut : WrongParserOutBase;

    public interface IFirstStaticBinder<T> where T : class
    {
        public static ValueTask<T?> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult<T?>(null);
    }

    public interface ISecondStaticBinder<T> where T : class
    {
        public static ValueTask<T?> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult<T?>(null);
    }

    public sealed class AmbiguousInterfaceRequest : IFirstStaticBinder<AmbiguousInterfaceRequest>, ISecondStaticBinder<AmbiguousInterfaceRequest>;

    public sealed class ClassMethodWinsRequest : IFirstStaticBinder<ClassMethodWinsRequest>, ISecondStaticBinder<ClassMethodWinsRequest>
    {
        public string Source => "derived";
        public static ValueTask<ClassMethodWinsRequest?> BindAsync(HttpContext context, ParameterInfo parameter) => ValueTask.FromResult<ClassMethodWinsRequest?>(new());
    }
}