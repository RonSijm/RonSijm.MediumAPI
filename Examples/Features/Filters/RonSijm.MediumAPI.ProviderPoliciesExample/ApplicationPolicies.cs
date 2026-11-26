using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ProviderPoliciesExample;

public static class CommandPolicy
{
    public static async Task<Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult>> RunAsync<TSuccess>(Func<Task<TSuccess>> operation, PolicyTrace trace) where TSuccess : IResult
    {
        trace.Entries.Add("common:enter");
        try
        {
            return await operation();
        }
        catch (AccessDeniedException)
        {
            trace.Entries.Add("common:forbidden");
            return TypedResults.Forbid();
        }
        catch (ResourceMissingException)
        {
            trace.Entries.Add("common:missing");
            return TypedResults.NotFound();
        }
        catch (CommandConflictException)
        {
            trace.Entries.Add("common:conflict");
            return ProviderProblems.Problem(409, "command-conflict", "The item changed. Reload and try again.");
        }
        catch (CommandValidationException)
        {
            trace.Entries.Add("common:validation");
            return ProviderProblems.Problem(400, "invalid-operation", "The request could not be completed.");
        }
        finally
        {
            trace.Entries.Add("common:exit");
        }
    }
}

public static class RemoteProviderPolicy
{
    // Preserve this inner application catch; only the surrounding common mapper is extracted.
    public static async Task<Results<TSuccess, ProblemHttpResult>> RunAsync<TSuccess>(Func<Task<TSuccess>> operation, PolicyTrace trace) where TSuccess : IResult
    {
        trace.Entries.Add("remote:enter");
        try
        {
            return await operation();
        }
        catch (RemoteProviderException)
        {
            trace.Entries.Add("remote:handled");
            return ProviderProblems.Remote();
        }
        finally
        {
            trace.Entries.Add("remote:exit");
        }
    }
}