using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
#endif

namespace Uno.Extensions.Navigation
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseNavigation(
        this IHostBuilder builder)
        {
            return builder
                .ConfigureServices(sp =>
                {
                    _ = sp.AddSingleton<INavigationMapping, NavigationMapping>()
                           .AddSingleton<INavigationAdapter, FrameNavigationAdapter>()
                           .AddSingleton<INavigationService, NavigationService>();
                });
        }
    }

}
