// ReSharper disable CheckNamespace

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WiseAuth;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWiseAuth<T>(this IServiceCollection services)
        where T : struct, Enum
    {
        if (EnumPowerOfTwoValidator.IsValid<T>())
        {
            services.AddSingleton<IAuthorizationHandler, WiseAuthorizationHandler<T>>();
            services.AddSingleton<IWiseAuthDetails, WiseAuthDetails<T>>();
        }
        else
        {
            throw new Exception($"Invalid power-of-two range for '{typeof(T).FullName}'.  Values must be greater than 0, start at 1, and double each time with no gaps (ie 1, 2, 8 where 4 is missing)");
        }

        // We only ever want one instance, so we're using TryAddSingleton
        services.TryAddSingleton<IWiseAuthService, WiseAuthService>();
    }
}