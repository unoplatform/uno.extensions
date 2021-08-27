#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
#endif

using System;

namespace Uno.Extensions.Navigation
{
    public interface INavigationService
    {
        NavigationResult Navigate(NavigationRequest request);
    }

    public static class NavigationServiceExtensions
    {
        public static NavigationResult NavigateToView<TView>(this INavigationService service, object sender)
        {
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(typeof(TView).Name, UriKind.Relative))));
        }
    }
}
