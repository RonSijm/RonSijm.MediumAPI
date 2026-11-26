using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public sealed record GuardedStartRequest(IReadOnlyList<int>? Items, string Outcome = "ok");

public sealed record GuardedStartedDto(long Id, int SelectedItems, Guid Scope);

public sealed record GuardedFactoryBuild(string DeclaringType, string MethodName, int ParameterIndex);

public sealed class FactoryRequestScope
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class FactoryBuildLedger
{
    public ConcurrentQueue<GuardedFactoryBuild> Entries { get; } = new();
}

/// <summary>Reads the actual original signature once per native pipeline build.</summary>
public sealed class SelectionGuardFactory : IMediumApiEndpointFilterFactory
{
    private SelectionGuardFactory()
    {
        throw new InvalidOperationException("Static factories must not be constructed.");
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var index = ResolveRequestIndex(context);
        context.ApplicationServices.GetRequiredService<FactoryBuildLedger>().Entries.Enqueue(new GuardedFactoryBuild(context.MethodInfo.DeclaringType!.FullName!, context.MethodInfo.Name, index));
        return invocation =>
        {
            var request = invocation.GetArgument<GuardedStartRequest>(index);
            // This inspection deliberately precedes the common exception policy.
            if (request.Items is null || request.Items.Count == 0)
            {
                Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult> rejected = ResourceProblems.Command(400, "invalid-selection", "Select at least one item.");
                return ValueTask.FromResult<object?>(rejected);
            }

            invocation.HttpContext.Response.Headers["X-Factory-Scope"] = invocation.HttpContext.RequestServices.GetRequiredService<FactoryRequestScope>().Id.ToString();
            return next(invocation);
        };
    }

    internal static int ResolveRequestIndex(EndpointFilterFactoryContext context)
    {
        if (context.TryGetArgumentIndex<GuardedStartRequest>(out var index))
        {
            return index;
        }

        // Retain the example's established detailed startup error only on the failure path.
        return RequestIndex(context.MethodInfo);
    }

    public static int RequestIndex(MethodInfo method)
    {
        var matching = method.GetParameters().Select((parameter, index) => (parameter, index)).Where(item => item.parameter.ParameterType == typeof(GuardedStartRequest)).ToArray();
        if (matching.Length != 1)
        {
            throw new InvalidOperationException($"Expected one {nameof(GuardedStartRequest)} parameter on {method.DeclaringType!.FullName}.{method.Name}; found {matching.Length}.");
        }

        return matching[0].index;
    }
}

/// <summary>Shows native argument replacement before the following guard and original handler.</summary>
public readonly struct ReplaceSelectionFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var index = SelectionGuardFactory.ResolveRequestIndex(context);
        return invocation =>
        {
            if (invocation.HttpContext.Request.Headers["X-Replace-Selection"] == "true")
            {
                var request = invocation.GetArgument<GuardedStartRequest>(index);
                invocation.Arguments[index] = request with { Items = new[] { 41, 43 } };
            }

            return next(invocation);
        };
    }
}

public static class GuardedStartBridge
{
    public static async Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> InvokeAsync(GuardedStartRequest request, OperationOutcomeService operations, FactoryRequestScope scope, HttpContext context, CancellationToken cancellationToken)
    {
        Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult> result;
        if (!await operations.OwnsAsync(request.Outcome == "late-conflict" ? "ok" : request.Outcome, cancellationToken))
        {
            result = TypedResults.NotFound();
        }
        else
        {
            // Keep the original partial catch after the ownership query.
            try
            {
                var outcome = await operations.ExecuteAsync(request.Outcome == "late-conflict" ? "conflict" : request.Outcome, cancellationToken);
                OperationOutcomeProblems.MarkReplay(context, outcome.Replayed);
                result = TypedResults.Ok(new GuardedStartedDto(outcome.Id, request.Items!.Count, scope.Id));
            }
            catch (ReplayConflictException exception)
            {
                result = OperationOutcomeProblems.Replay(exception);
            }
        }

        return result;
    }
}

public sealed class FactoryStageAttribute(string value) : Attribute, IEndpointMetadataAttribute
{
    public string Value { get; } = value;
}

[FactoryStage("declared")]
[Consumes(typeof(GuardedStartRequest), "application/json")]
[DisableEndpointAntiforgery]
[EndpointResponse(418)]
[EndpointResponseFallback(419)]
[ResponseHeader("X-Factory-Declaration", "present")]
public sealed class DeclaredFactory : IMediumApiEndpointFilterFactory
{
    private DeclaredFactory()
    {
        throw new InvalidOperationException("Static factories must not be constructed.");
    }

    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.Add(builder => builder.Metadata.Add(new FactoryStageAttribute("configure")));
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return invocation =>
        {
            invocation.HttpContext.Response.Headers["X-Factory-Created-For"] = context.MethodInfo.Name;
            return next(invocation);
        };
    }
}

// A struct with no Configure override uses the interface's default no-op convention.
public readonly struct RepeatedFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return async invocation =>
        {
            var count = invocation.HttpContext.Items.TryGetValue("factory-count", out var value) ? (int)value! : 0;
            invocation.HttpContext.Items["factory-count"] = count + 1;
            var result = await next(invocation);
            invocation.HttpContext.Response.Headers["X-Factory-Count"] = ((int)invocation.HttpContext.Items["factory-count"]!).ToString();
            return result;
        };
    }
}

public sealed class FactoryOrderFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers["X-Regular-Entered"] = "true";
        context.HttpContext.Response.Headers["X-Regular-Factory-Count"] = context.HttpContext.Items.TryGetValue("factory-count", out var count) ? ((int)count!).ToString() : "0";
        return await next(context);
    }
}

public readonly struct NextTwiceFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return async invocation =>
        {
            await next(invocation);
            return await next(invocation);
        };
    }
}