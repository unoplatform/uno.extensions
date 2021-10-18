using System;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
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

        scopedServices.GetService<RegionControlProvider>().RegionControl = services.GetService<RegionControlProvider>().RegionControl;
        scopedServices.AddInstance<INavigator>(new InnerNavigator( services.GetInstance<INavigator>()));

        return scopedServices;
    }
}
