using System.Reflection;

namespace RonSijm.MediumAPI;

/// <summary>Reads only native header-family attributes and flattens sets in the framework's existing declaration and method-inheritance order.</summary>
internal static class ResponseHeaderDeclarations
{
    internal static IEnumerable<ResponseHeaderAttribute> Read(MemberInfo member, bool inherit)
    {
        if (!member.IsDefined(typeof(ResponseHeadersAttribute), inherit))
        {
            return member.GetCustomAttributes<ResponseHeaderAttribute>(inherit);
        }

        return Flatten(member.GetCustomAttributes<ResponseHeaderDeclarationAttribute>(inherit));
    }

    private static IEnumerable<ResponseHeaderAttribute> Flatten(IEnumerable<ResponseHeaderDeclarationAttribute> declarations)
    {
        foreach (var declaration in declarations)
        {
            if (declaration is ResponseHeaderAttribute header) { yield return header; }
            else if (declaration is ResponseHeadersAttribute headers)
            {
                foreach (var child in headers.Headers) { yield return child; }
            }
        }
    }
}