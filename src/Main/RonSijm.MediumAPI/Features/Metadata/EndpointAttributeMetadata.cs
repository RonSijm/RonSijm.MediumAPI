using System.Reflection;

namespace RonSijm.MediumAPI;

/// <summary>Constructs one selected attribute with its declared constructor and named arguments without caching instances or inspecting unrelated declarations.</summary>
internal static class EndpointAttributeMetadata
{
    internal static object Create(CustomAttributeData declaration)
    {
        var metadata = declaration.Constructor.Invoke(declaration.ConstructorArguments.Select(ReadValue).ToArray());
        foreach (var argument in declaration.NamedArguments)
        {
            var value = ReadValue(argument.TypedValue);
            if (argument.MemberInfo is FieldInfo field)
            {
                field.SetValue(metadata, value);
            }
            else
            {
                ((PropertyInfo)argument.MemberInfo).SetValue(metadata, value);
            }
        }

        return metadata;
    }

    private static object? ReadValue(CustomAttributeTypedArgument argument)
    {
        if (argument.Value is CustomAttributeTypedArgument boxed)
        {
            return ReadValue(boxed);
        }

        if (argument.Value is IEnumerable<CustomAttributeTypedArgument> elements)
        {
            var values = elements.ToArray();
            var array = Array.CreateInstance(argument.ArgumentType.GetElementType()!, values.Length);
            for (var index = 0; index < values.Length; index++)
            {
                array.SetValue(ReadValue(values[index]), index);
            }

            return array;
        }

        return argument.ArgumentType.IsEnum && argument.Value is not null
            ? Enum.ToObject(argument.ArgumentType, argument.Value)
            : argument.Value;
    }
}