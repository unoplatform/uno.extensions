using System;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation;

public static class NavigatorFactoryExtensions
{
    public static Type FindServiceByType(this NavigatorFactory factory, Type viewType)
    {
        if (viewType is null)
        {
            return null;
        }

        if (factory.ServiceTypes.TryGetValue(viewType.Name, out var serviceType))
        {
            return serviceType;
        }
        var baseTypes = viewType.GetBaseTypes();
        foreach (var type in baseTypes)
        {
            if (factory.ServiceTypes.TryGetValue(type.Name, out var baseServiceType))
            {
                return baseServiceType;
            }

        }

        return null;
    }
}
