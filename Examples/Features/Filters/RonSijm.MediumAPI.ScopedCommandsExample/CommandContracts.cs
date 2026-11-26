using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ScopedCommandsExample;

public sealed record ResourceReceipt(int Id, string Source);

public sealed record CommandRequest(string? Outcome);

public sealed class AccessDeniedException : Exception { }

public sealed class ResourceMissingException : Exception { }

public sealed class CommandConflictException : Exception { }

public sealed class ConfirmationRequiredException : Exception { }

public sealed class UnexpectedCommandException : Exception { }

public sealed class CommandValidationException(string? code) : Exception("Internal synthetic detail must not be returned.")
{
    public string? Code { get; } = code;
}

public sealed class CommandDomainException : Exception
{
    public CommandDomainException() : base("Internal synthetic detail must not be returned.") { }
}

public static class CommandProblems
{
    public static ProblemHttpResult Problem(int status, string code, string title)
    {
        return TypedResults.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    public static ProblemHttpResult Validation(CommandValidationException exception)
    {
        return Problem(400, exception.Code ?? "invalid-request", "The request could not be completed.");
    }

    public static ProblemHttpResult Conflict()
    {
        return Problem(409, "command-conflict", "The item changed. Reload and try again.");
    }

    public static ProblemHttpResult Confirmation()
    {
        return Problem(409, "confirmation-required", "Confirm your identity before changing the release.");
    }
}