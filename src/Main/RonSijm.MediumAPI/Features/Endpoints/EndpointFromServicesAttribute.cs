namespace RonSijm.MediumAPI;

/// <summary>Resolves the mapped endpoint type from the current request service provider when its handler target is needed.</summary>
/// <remarks>
/// Register the mapped concrete type explicitly with the host's dependency injection container.
/// The adapter never disposes a resolved target; its registration and native service-provider or application ownership control cleanup.
/// This declaration is not inherited and does not register services or create a service scope.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EndpointFromServicesAttribute : Attribute { }