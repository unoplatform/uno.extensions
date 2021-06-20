using System;
using System.Collections.Generic;

namespace Uno.Extensions.Navigation
{
    public static class RouteTypeExtensions
    {
        public static string AsRoute(this Type routeViewModel) =>
            routeViewModel?.Name
#pragma warning disable CA1304, IDE0079 // Specify CultureInfo
                    ?.ToLower()
#pragma warning restore CA1304 // Specify CultureInfo
#pragma warning disable CA1307 // Specify StringComparison for clarity
                    ?.Replace("pageviewmodel", string.Empty);
#pragma warning restore CA1307, IDE0079 // Specify StringComparison for clarity

        public static Dictionary<string, (Type, Type)> RegisterPage<TViewModel, TPage>(this Dictionary<string, (Type, Type)> routeDictionary, string path = null)
        {
            if (routeDictionary == null)
            {
                throw new ArgumentNullException(nameof(routeDictionary));
            }

            if (path != null)
            {
                routeDictionary[path] = (typeof(TPage), typeof(TViewModel));
            }
            routeDictionary[typeof(TViewModel).AsRoute()] = (typeof(TPage), typeof(TViewModel));
            return routeDictionary;
        }
    }
}
