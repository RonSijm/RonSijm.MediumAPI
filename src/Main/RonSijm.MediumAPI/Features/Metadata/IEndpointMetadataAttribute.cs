namespace RonSijm.MediumAPI;

/// <summary>Explicitly opts an attribute into native endpoint metadata when declared on an endpoint class, namespace group definition or registered filter class.</summary>
/// <remarks>Class declarations are read directly without CLR inheritance. Original handler attributes retain ASP.NET Core's native behavior. This marker does not execute MVC filters or metadata-provider methods.</remarks>
public interface IEndpointMetadataAttribute { }