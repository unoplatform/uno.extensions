using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNavigation(
         this IServiceCollection services
     )
    {

        return services.AddSingleton<INavigationMapping, NavigationMapping>()
                       .AddSingleton<IFrameWrapper, FrameWrapper>()
                       .AddSingleton<INavigationAdapter, FrameNavigationAdapter>()
                       .AddSingleton<INavigationService, NavigationService>();
    }
}

