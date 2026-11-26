using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.InputPoliciesExample;

public sealed record InputRequest(string? Outcome);

public sealed record MembershipReceipt(int Id);

public sealed record IntakeReceipt(int Id);

public abstract class SampleApplicationException : Exception
{
    protected SampleApplicationException() : base("Internal synthetic detail must not be returned.") { }
}

public sealed class ConfirmationRequiredException : SampleApplicationException { }

public sealed class CommandValidationException(string? code) : SampleApplicationException
{
    public string? Code { get; } = code;
}

public sealed class FieldInputException(string code, string field) : SampleApplicationException
{
    public string Code { get; } = code;
    public string Field { get; } = field;
}

public sealed class AccessDeniedException : SampleApplicationException { }

public sealed class ResourceMissingException : SampleApplicationException { }

public sealed class CommandConflictException : SampleApplicationException { }

public sealed class UnexpectedInputException : Exception { }

public sealed class InputTrace
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    public List<string> Entries { get; } = [];
}

public sealed class InputProbe
{
    private int guards;
    private int operations;
    public int Guards => Volatile.Read(ref guards);
    public int Operations => Volatile.Read(ref operations);

    public void Guard()
    {
        Interlocked.Increment(ref guards);
    }

    public void Operation()
    {
        Interlocked.Increment(ref operations);
    }
}

public static class InputProblems
{
    public const string PendingCode = "invitation-pending";

    public static ProblemHttpResult Problem(int status, string code, string title)
    {
        return TypedResults.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    public static ProblemHttpResult Input(string code, string field, int status = 400)
    {
        return TypedResults.Problem(statusCode: status, title: "Check the submitted input.", extensions: new Dictionary<string, object?> { ["code"] = code, ["field"] = field });
    }
}

public static class SyntheticReport
{
    public const string MediaType = "application/octet-stream";
    public const string DownloadName = "sample-report.bin";

    public static byte[] Content()
    {
        return Encoding.UTF8.GetBytes("SYNTHETIC-REPORT\n");
    }
}