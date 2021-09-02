using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;

namespace Uno.Extensions.Navigation
{
    public static class NavigationServiceExtensions
    {
        public static NavigationResult NavigateToView<TView>(this INavigationService service, object sender, object data = null)
        {
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByView(typeof(TView));
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data)));
        }

        public static NavigationResult<TResponse> NavigateToView<TView, TResponse>(this INavigationService service, object sender, object data = null)
        {
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByView(typeof(TView));
            var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data), typeof(TResponse)));
            return new NavigationResult<TResponse>(result.Request, result.NavigationTask, result.Response.ContinueWith(x => (TResponse)x.Result));
        }

        public static NavigationResult NavigateToViewModel<TViewViewModel>(this INavigationService service, object sender, object data = null)
        {
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByViewModel(typeof(TViewViewModel));
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data)));
        }
        public static NavigationResult<TResponse> NavigateToViewModel<TViewViewModel, TResponse>(this INavigationService service, object sender, object data = null)
        {
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByViewModel(typeof(TViewViewModel));
            var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data), typeof(TResponse)));
            return new NavigationResult<TResponse>(result.Request, result.NavigationTask, result.Response.ContinueWith(x => (TResponse)x.Result));
        }

        public static NavigationResult NavigateForData<TData>(this INavigationService service, object sender, TData data)
        {
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByData(typeof(TData));
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data)));
        }

        public static NavigationResult NavigateToPreviousView(this INavigationService service, object sender, object data = null)
        {
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(FrameNavigationAdapter.PreviousViewUri, UriKind.Relative), data)));
        }
    }
}
