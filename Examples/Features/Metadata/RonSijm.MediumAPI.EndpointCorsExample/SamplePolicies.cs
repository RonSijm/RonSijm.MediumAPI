namespace RonSijm.MediumAPI.EndpointCorsExample;

public static class SamplePolicies
{
    public const string Browser = "Browser";
    public const string Override = "Override";
    public const string DefaultOrigin = "https://default.example";
    public const string BrowserOrigin = "https://client.example";
    public const string OtherBrowserOrigin = "https://other-client.example";
    public const string OverrideOrigin = "https://override.example";
    public const string RejectedOrigin = "https://untrusted.example";
    public const string RequestHeader = "X-Client";
    public const string DefaultRequestHeader = "X-Default";
    public const string ResponseHeader = "X-Sample-Value";
}