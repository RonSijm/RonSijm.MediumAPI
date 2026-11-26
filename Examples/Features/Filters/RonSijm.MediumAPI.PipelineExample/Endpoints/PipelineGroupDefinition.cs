using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.PipelineExample.Endpoints;

public sealed class PipelineGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/pipeline/{tenantId:int}").AddEndpointFilterFactory(PipelineFilters.Increment);
}

public static class PipelineFilters
{
    public static EndpointFilterDelegate Increment(EndpointFilterFactoryContext factory, EndpointFilterDelegate next)
    {
        var probe = factory.ApplicationServices.GetRequiredService<LifetimeProbe>();
        probe.FilterMethods.Enqueue(factory.MethodInfo);
        var index = FindValue(factory.MethodInfo);
        if (index < 0)
        {
            return next;
        }

        return async context =>
        {
            var value = context.GetArgument<int>(index);
            if (value < 0)
            {
                return Results.BadRequest("Choose a nonnegative sample value.");
            }

            if (value == 99)
            {
                throw new ExampleFailureException();
            }

            context.Arguments[index] = value + 10;
            return await next(context);
        };
    }

    public static EndpointFilterDelegate Double(EndpointFilterFactoryContext factory, EndpointFilterDelegate next) => Scale(factory, next, 2);
    public static EndpointFilterDelegate Triple(EndpointFilterFactoryContext factory, EndpointFilterDelegate next) => Scale(factory, next, 3);

    private static EndpointFilterDelegate Scale(EndpointFilterFactoryContext factory, EndpointFilterDelegate next, int factor)
    {
        var index = FindValue(factory.MethodInfo);
        if (index < 0)
        {
            return next;
        }

        return async context =>
        {
            context.Arguments[index] = context.GetArgument<int>(index) * factor;
            return await next(context);
        };
    }

    private static int FindValue(MethodInfo method) => Array.FindIndex(method.GetParameters(), parameter => (parameter.GetCustomAttribute<FromRouteAttribute>()?.Name ?? parameter.Name) == "value");
}