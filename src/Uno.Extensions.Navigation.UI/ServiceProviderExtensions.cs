using System;
using Microsoft.Extensions.DependencyInjection;
#if !WINUI
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation;

public static class ServiceProviderExtensions
{
    public static IServiceProvider CloneNavigationScopedServices(this IServiceProvider services)
    {
        var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        scopedServices.GetRequiredService<RegionControlProvider>().RegionControl = services.GetRequiredService<RegionControlProvider>().RegionControl;
        var instance = services.GetInstance<INavigator>();
        if (instance is not null)
        {
            scopedServices.AddInstance<INavigator>(instance);
        }

        return scopedServices;
    }
}
