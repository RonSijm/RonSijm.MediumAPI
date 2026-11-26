using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI;

public interface IEndpoint
{
    HttpVerb Verb { get; }

    [StringSyntax("Route")]
    string Pattern { get; }

    string? AuthorizationPolicy => null;

    /// <summary>Optional legacy named partition. Namespace groups do not require endpoint membership metadata.</summary>
    string? RegistrationGroup => null;
}