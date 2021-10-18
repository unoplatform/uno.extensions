using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions
{
    public static class RegionExtensions
    {
        public static INavigator Navigation(this IRegion region) => region.Services?.GetService<INavigator>();

        public static INavigatorFactory NavigationFactory(this IRegion region) => region.Services?.GetService<INavigatorFactory>();

        public static Task<NavigationResponse> NavigateAsync(this IRegion region, NavigationRequest request) => region.Navigation()?.NavigateAsync(request);

    }
}
