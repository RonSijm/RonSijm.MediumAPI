using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.MediumAPI.Analyser;

/// <summary>Reads only values proven constant by the declaring compilation; never executes property getters.</summary>
internal static class EndpointConstantValues
{
    internal static bool TryGetString(Compilation compilation, ExpressionSyntax? expression, bool allowNull, out string? value)
    {
        value = null;
        if (expression is null || !compilation.ContainsSyntaxTree(expression.SyntaxTree))
        {
            return false;
        }

        var constant = compilation.GetSemanticModel(expression.SyntaxTree).GetConstantValue(expression);
        if (!constant.HasValue)
        {
            return false;
        }

        if (constant.Value is string text)
        {
            value = text;
            return true;
        }

        return allowNull && constant.Value is null;
    }
}