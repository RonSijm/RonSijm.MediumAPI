using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.ResourcePoliciesHost;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest;

public sealed class FactoryArgumentLookupTests
{
    [Fact]
    public void Type_only_lookup_requires_one_exact_CLR_type_including_nullable_value_types()
    {
        var context = Context(typeof(Signatures).GetMethod(nameof(Signatures.Mixed))!);
        Assert.True(context.TryGetArgumentIndex<int>(out var required));
        Assert.Equal(2, required);
        Assert.True(context.TryGetArgumentIndex<int?>(out var optional));
        Assert.Equal(3, optional);
        Assert.True(context.TryGetArgumentIndex<DerivedArgument>(out var derived));
        Assert.Equal(4, derived);
        Assert.False(context.TryGetArgumentIndex<BaseArgument>(out var assignable));
        Assert.Equal(-1, assignable);
        Assert.False(context.TryGetArgumentIndex<long>(out var nonNullable));
        Assert.Equal(-1, nonNullable);
    }

    [Theory]
    [InlineData("customerId", 0)]
    [InlineData("projectId", 1)]
    public void Named_lookup_disambiguates_repeated_types_using_the_CLR_parameter_name(string name, int expected)
    {
        var context = Context(typeof(Signatures).GetMethod(nameof(Signatures.Mixed))!);
        Assert.True(context.TryGetArgumentIndex<long?>(name, out var index));
        Assert.Equal(expected, index);
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("CustomerId")]
    [InlineData("")]
    [InlineData("missing")]
    public void Binding_aliases_case_changes_and_missing_names_do_not_match(string name)
    {
        var context = Context(typeof(Signatures).GetMethod(nameof(Signatures.Mixed))!);
        Assert.False(context.TryGetArgumentIndex<long?>(name, out var index));
        Assert.Equal(-1, index);
    }

    [Fact]
    public void Every_failed_lookup_resets_the_output_for_missing_ambiguous_and_wrong_typed_names()
    {
        var context = Context(typeof(Signatures).GetMethod(nameof(Signatures.Mixed))!);
        var index = 999;
        Assert.False(context.TryGetArgumentIndex<DateTime>(out index));
        Assert.Equal(-1, index);
        index = 999;
        Assert.False(context.TryGetArgumentIndex<long?>(out index));
        Assert.Equal(-1, index);
        index = 999;
        Assert.False(context.TryGetArgumentIndex<int?>("customerId", out index));
        Assert.Equal(-1, index);
    }

    [Fact]
    public void Shifted_inherited_closed_generic_method_metadata_keeps_its_native_position()
    {
        var method = typeof(ClosedGenericSignatures).GetMethod(nameof(GenericSignatures<long?>.Parameters))!;
        Assert.Equal(typeof(GenericSignatures<long?>), method.DeclaringType);
        var context = Context(method);
        Assert.True(context.TryGetArgumentIndex<long?>(out var index));
        Assert.Equal(1, index);
        Assert.True(context.TryGetArgumentIndex<long?>("request", out index));
        Assert.Equal(1, index);
        Assert.False(context.TryGetArgumentIndex<long>(out index));
        Assert.Equal(-1, index);
    }

    [Fact]
    public void Reference_nullability_is_erased_in_CLR_type_matching()
    {
        var context = Context(typeof(GenericSignatures<string>).GetMethod(nameof(GenericSignatures<string>.Parameters))!);
        Assert.True(context.TryGetArgumentIndex<string>("request", out var index));
        Assert.Equal(1, index);
        Assert.False(context.TryGetArgumentIndex<string>(out index));
        Assert.Equal(-1, index);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Null_context_is_rejected_before_any_lookup(bool named)
    {
        EndpointFilterFactoryContext context = null!;
        var error = named ? Assert.Throws<ArgumentNullException>(() => context.TryGetArgumentIndex<int>("value", out _)) : Assert.Throws<ArgumentNullException>(() => context.TryGetArgumentIndex<int>(out _));
        Assert.Equal("context", error.ParamName);
    }

    [Fact]
    public void Null_name_is_rejected_and_null_context_keeps_validation_precedence()
    {
        var context = Context(typeof(Signatures).GetMethod(nameof(Signatures.Mixed))!);
        Assert.Equal("parameterName", Assert.Throws<ArgumentNullException>(() => context.TryGetArgumentIndex<int>(null!, out _)).ParamName);
        EndpointFilterFactoryContext absent = null!;
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => absent.TryGetArgumentIndex<int>(null!, out _)).ParamName);
    }

    [Theory]
    [InlineData(nameof(Signatures.NoSelection), 0)]
    [InlineData(nameof(Signatures.AmbiguousSelection), 2)]
    public void Existing_factory_startup_errors_keep_their_original_handler_identity_and_count(string methodName, int matches)
    {
        var method = typeof(Signatures).GetMethod(methodName)!;
        var error = Assert.Throws<InvalidOperationException>(() => SelectionGuardFactory.Create(Context(method), _ => ValueTask.FromResult<object?>(TypedResults.Ok())));
        Assert.Equal($"Expected one {nameof(GuardedStartRequest)} parameter on {typeof(Signatures).FullName}.{methodName}; found {matches}.", error.Message);
    }

    [Fact]
    public async Task Captured_lookup_reads_the_current_replaced_argument_and_keeps_early_union_identity()
    {
        var method = typeof(Signatures).GetMethod(nameof(Signatures.ShiftedProfile))!;
        var nextCalled = false;
        var expected = TypedResults.Ok(71);
        var guard = ProfileArgumentGuardFactory.Create(Context(method), invocation =>
        {
            nextCalled = true;
            Assert.Equal("Replacement", invocation.GetArgument<GuardProfileRequest>(2).Profile!.DisplayName);
            return ValueTask.FromResult<object?>(expected);
        });
        var invocation = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new DefaultHttpContext(), 7L, new GuardProfileRequest(null));
        var rejected = await guard(invocation);
        Assert.IsType<BadRequest>(Assert.IsType<Results<BadRequest, Ok<GuardProfileDto>>>(rejected).Result);
        Assert.False(nextCalled);
        invocation.Arguments[2] = new GuardProfileRequest(new GuardProfileDto("Replacement"));
        Assert.Same(expected, await guard(invocation));
        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData(true, "ok", 400, 0)]
    [InlineData(false, "ok", 200, 1)]
    [InlineData(false, "validation", 400, 1)]
    [InlineData(false, "forbidden", 403, 1)]
    public async Task Profile_guard_stays_inside_common_mapping_and_preserves_typed_results(bool missing, string outcome, int status, int operations)
    {
        Contract? reference = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsJsonAsync("/sample/argument-guards/profile", new GuardProfileRequest(missing ? null : new GuardProfileDto("Synthetic profile"), outcome));
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(operations, Probe(app).Operations);
            Assert.Null(response.Headers.CacheControl);
            var contract = await Capture(response);
            if (legacy) { reference = contract; }
            else { Equal(reference!, contract); }

            if (missing || status == 403) { Assert.Empty(contract.Body); }
            else if (status == 200) { Assert.Equal("Synthetic profile", (await response.Content.ReadFromJsonAsync<GuardProfileDto>())!.DisplayName); }
            else { AssertProblem(contract, "invalid-request"); }
        }
    }

    [Theory]
    [InlineData("", 200, 1)]
    [InlineData("project=7", 400, 0)]
    [InlineData("customer=3", 200, 1)]
    [InlineData("customer=3&project=7", 200, 1)]
    [InlineData("customer=0&project=-2", 200, 1)]
    public async Task Named_nullable_scope_guard_preserves_only_the_existing_consistency_rule(string query, int status, int reads)
    {
        Contract? reference = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/sample/argument-guards/support?" + query);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(reads, Probe(app).Reads);
            Assert.Null(response.Headers.CacheControl);
            var contract = await Capture(response);
            if (legacy) { reference = contract; }
            else { Equal(reference!, contract); }

            if (status == 400)
            {
                using var body = JsonDocument.Parse(contract.Body);
                Assert.Equal("projectId requires customerId.", body.RootElement.GetProperty("error").GetString());
                Assert.Equal("application/json", contract.MediaType);
            }
        }
    }

    [Theory]
    [InlineData("runningPage=0", 400, 0, false)]
    [InlineData("waitingPage=-1", 400, 0, false)]
    [InlineData("finishedPage=100001", 400, 0, false)]
    [InlineData("projectPage=0", 400, 0, false)]
    [InlineData("runningPage=1&waitingPage=100000", 200, 2, true)]
    [InlineData("", 200, 2, true)]
    [InlineData("runningPage=0&outcome=later-validation", 400, 0, false)]
    [InlineData("outcome=first-validation", 400, 1, false)]
    [InlineData("outcome=later-validation", 400, 2, true)]
    [InlineData("outcome=first-missing", 404, 1, false)]
    [InlineData("outcome=later-missing", 404, 2, true)]
    public async Task Page_guards_keep_common_translation_and_the_original_mid_operation_header(string query, int status, int reads, bool noStore)
    {
        Contract? reference = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/sample/argument-guards/overview?" + query);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(reads, Probe(app).Reads);
            Assert.Equal(noStore, response.Headers.CacheControl?.NoStore == true);
            var contract = await Capture(response);
            if (legacy) { reference = contract; }
            else { Equal(reference!, contract); }

            if (status == 400) { AssertProblem(contract, "invalid-request"); }

            if (status == 200)
            {
                using var body = JsonDocument.Parse(contract.Body);
                Assert.Equal(17, body.RootElement.GetProperty("state").GetInt32());
                Assert.Equal(23, body.RootElement.GetProperty("settings").GetInt32());
            }
        }
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("support")]
    [InlineData("overview")]
    public async Task Earlier_native_rejection_does_not_run_guards_or_operations(string action)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(action == "profile" ? HttpMethod.Post : HttpMethod.Get, "/sample/argument-guards/" + action);
            if (action == "profile") { request.Content = JsonContent.Create(new GuardProfileRequest(null)); }

            request.Headers.Add("X-Sample-Block", "true");
            using var response = await client.SendAsync(request);
            Assert.Equal(403, (int)response.StatusCode);
            Assert.Equal(0, Probe(app).Operations);
            Assert.Equal(0, Probe(app).Reads);
            Assert.Null(response.Headers.CacheControl);
        }
    }

    [Fact]
    public async Task Native_metadata_and_API_descriptions_retain_the_original_named_guard_contracts()
    {
        await using var legacy = Create(true);
        await using var medium = Create(false);
        await legacy.StartAsync();
        await medium.StartAsync();
        Assert.Equal(Descriptions(legacy), Descriptions(medium));
        foreach (var item in new[] { ("/sample/argument-guards/profile", "POST"), ("/sample/argument-guards/support", "GET"), ("/sample/argument-guards/overview", "GET") })
        {
            var original = Route(legacy, item.Item1, item.Item2);
            var actual = Route(medium, item.Item1, item.Item2);
            Assert.Equal(Responses(original), Responses(actual));
            Assert.Equal(original.Metadata.GetMetadata<MethodInfo>()!.ReturnType, actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
        }

        foreach (var factory in new[] { typeof(ProfileArgumentGuardFactory), typeof(SupportArgumentGuardFactory), typeof(OverviewArgumentGuardFactory) })
        {
            Assert.Empty(factory.GetCustomAttributes<EndpointResponseAttribute>(false));
            Assert.Empty(factory.GetCustomAttributes<EndpointResponseFallbackAttribute>(false));
        }
    }

    private static EndpointFilterFactoryContext Context(MethodInfo method)
    {
        return new EndpointFilterFactoryContext { MethodInfo = method, ApplicationServices = new EmptyServices() };
    }

    private static async Task<Contract> Capture(HttpResponseMessage response)
    {
        return new Contract(await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType?.MediaType);
    }

    private static void Equal(Contract expected, Contract actual)
    {
        Assert.Equal(expected.Body, actual.Body);
        Assert.Equal(expected.MediaType, actual.MediaType);
    }

    private static void AssertProblem(Contract contract, string code)
    {
        Assert.Equal("application/problem+json", contract.MediaType);
        using var body = JsonDocument.Parse(contract.Body);
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
        Assert.Equal("The request could not be completed.", body.RootElement.GetProperty("title").GetString());
    }

    private sealed record Contract(byte[] Body, string? MediaType);

    private sealed class EmptyServices : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }

    public class BaseArgument;

    public sealed class DerivedArgument : BaseArgument;

    public static class Signatures
    {
        public static void Mixed([FromQuery(Name = "customer")] long? customerId, long? projectId, int page, int? runningPage, DerivedArgument concrete) { }

        public static void NoSelection(HttpContext context) { }

        public static void AmbiguousSelection(GuardedStartRequest first, GuardedStartRequest second) { }

        public static void ShiftedProfile(HttpContext context, long id, GuardProfileRequest request) { }
    }

    public class GenericSignatures<T>
    {
        public void Parameters(string label, T request, int? page) { }
    }

    public sealed class ClosedGenericSignatures : GenericSignatures<long?>;
}