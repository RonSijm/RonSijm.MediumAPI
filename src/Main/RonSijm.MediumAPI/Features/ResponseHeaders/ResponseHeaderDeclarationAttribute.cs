namespace RonSijm.MediumAPI;

/// <summary>Common native attribute family for a single header or an ordered header set. Applications derive from ResponseHeaderAttribute or ResponseHeadersAttribute.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class ResponseHeaderDeclarationAttribute : Attribute
{
    internal ResponseHeaderDeclarationAttribute() { }
}