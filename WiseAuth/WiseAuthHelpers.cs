using System.Text;
using Microsoft.AspNetCore.Builder;

namespace WiseAuth;

public static class WiseAuthHelpers
{
    public static RouteHandlerBuilder EndpointId<T>(
        this RouteHandlerBuilder builder,
        T endpointId)
        where T : Enum
    {
        return builder
            .WithMetadata(new WiseAuthMetadata<T>(endpointId))
            .RequireAuthorization();
    }

    internal static string GetClaimType(Type enumType)
    {
        return GetCustomClaimType(enumType) ?? GetClaimType(enumType.FullName);
    }

    private static string GetClaimType(string? enumFullName)
    {
        if (enumFullName is null)
        {
            return string.Empty;
        }

        var lastDotIdx = enumFullName.LastIndexOf('.');
        var typeName = lastDotIdx == -1 ? enumFullName : enumFullName[(lastDotIdx + 1)..];

        var values = typeName.Split('+');
        var valuesLengthMinusOne = values.Length - 1;

        StringBuilder result = new();
        for (var idx = 0; idx < values.Length; idx++)
        {
            var value = values[idx];
            result.Append(idx < valuesLengthMinusOne
                ? CapitalizeFirstCharacter(value.Replace("Controller", string.Empty))
                : CapitalizeFirstCharacter(value));
        }

        return result.ToString();
    }

    private static string CapitalizeFirstCharacter(string s)
    {
        return s.Length == 0 ? s : $"{char.ToUpper(s[0])}{s[1..]}";
    }

    private static string? GetCustomClaimType(Type enumType)
    {
        var attribute = Attribute.GetCustomAttribute(enumType, typeof(ClaimTypeAttribute)) as ClaimTypeAttribute;
        return attribute?.Name;
    }
}
