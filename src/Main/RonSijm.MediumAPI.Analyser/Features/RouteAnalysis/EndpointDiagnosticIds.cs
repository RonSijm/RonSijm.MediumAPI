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
    public const string DuplicateNamespaceGroup = "ENDPOINT009";
    public const string InvalidEndpointHandler = "ENDPOINT010";
    public const string InvalidEndpointConfigure = "ENDPOINT011";
    public const string InvalidEndpointDeclaration = "ENDPOINT012";
    public const string InvalidNamespaceGroupDeclaration = "ENDPOINT013";
    public const string InheritedRouteParameterMissingFromLayout = "ENDPOINT014";
    public const string InheritedRouteConstraintTypeMismatch = "ENDPOINT015";
    public const string InvalidEndpointPatternDeclaration = "ENDPOINT016";
}