using System;

namespace Uno.Extensions.Navigation
{
    public static class NavigationServiceExtensions
    {
        public static NavigationResult NavigateToView<TView>(this INavigationService service, object sender)
        {
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(typeof(TView).Name, UriKind.Relative))));
        }

        public static NavigationResult NavigateToPreviousView(this INavigationService service, object sender)
        {
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(FrameNavigationAdapter.PreviousViewUri, UriKind.Relative))));
        }
    }
}
