namespace RonSijm.MediumAPI.Analyser;

public static class EndpointDiagnosticIds
{
	public const string RouteParameterMissingFromHandler = "ENDPOINT001";
	public const string HandlerParameterMissingFromRoute = "ENDPOINT002";
	public const string RouteConstraintTypeMismatch = "ENDPOINT003";
	public const string EndpointLambdaShouldBeStatic = "ENDPOINT004";
	public const string PatternPropertyMissingStringSyntaxAttribute = "ENDPOINT005";
	public const string HandleAsyncMethodMissing = "ENDPOINT007";
	public const string EndpointMetadataUnparseable = "ENDPOINT008";
}
