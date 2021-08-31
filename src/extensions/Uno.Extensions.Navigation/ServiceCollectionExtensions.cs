using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNavigation(this IServiceCollection services)
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddSingleton<INavigationMapping, NavigationMapping>()
                    .AddSingleton<IFrameWrapper, FrameWrapper>()
                    .AddSingleton<ITabWrapper, TabWrapper>()
                    //.AddSingleton<INavigationAdapter, FrameNavigationAdapter>()
                    .AddSingleton<INavigationAdapter, TabNavigationAdapter>()
                    .AddSingleton<INavigationService, NavigationService>();
    }
}
