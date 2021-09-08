using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;

namespace Uno.Extensions.Navigation
{
    public static class NavigationServiceExtensions
    {
        public static NavigationResult NavigateToView<TView>(this INavigationService service, object sender, object data = null)
        {
            return service.NavigateToView(sender, typeof(TView), data);
        }

        public static NavigationResult NavigateToView(this INavigationService service, object sender, Type viewType, object data = null)
        { 
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByView(viewType);
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data)));
        }

        public static NavigationResult<TResponse> NavigateToView<TView, TResponse>(this INavigationService service, object sender, object data = null)
        {
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByView(typeof(TView));
            var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data), typeof(TResponse)));
            return new NavigationResult<TResponse>(result.Request, result.NavigationTask, result.CancellationSource, result.Response.ContinueWith(x => (TResponse)x.Result));
        }

        public static NavigationResult NavigateToViewModel<TViewViewModel>(this INavigationService service, object sender, object data = null)
        {
            return service.NavigateToViewModel(sender, typeof(TViewViewModel), data);
        }

        public static NavigationResult NavigateToViewModel(this INavigationService service, object sender, Type viewModelType, object data = null)
        {

            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByViewModel(viewModelType);
            return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data)));
        }

        public static NavigationResult<TResponse> NavigateToViewModel<TViewViewModel, TResponse>(this INavigationService service, object sender, object data = null)
        {
            var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
            var map = mapping.LookupByViewModel(typeof(TViewViewModel));
            var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.Path, UriKind.Relative), data), typeof(TResponse)));
            return new NavigationResult<TResponse>(result.Request, result.NavigationTask, result.CancellationSource, result.Response.ContinueWith(x => (TResponse)x.Result));
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

        public static NavigationResult<Windows.UI.Popups.UICommand> ShowMessageDialog(
            this INavigationService service,
            object sender,
            string content,
            string title = null,
            MessageDialogOptions options = MessageDialogOptions.None,
            uint defaultCommandIndex = 0,
            uint cancelCommandIndex = 0,
            params Windows.UI.Popups.UICommand[] commands)
        {
            var data = new Dictionary<string, object>()
            {
                { FrameNavigationAdapter.MessageDialogParameterTitle, title },
                { FrameNavigationAdapter.MessageDialogParameterContent, content },
                { FrameNavigationAdapter.MessageDialogParameterOptions, options },
                { FrameNavigationAdapter.MessageDialogParameterDefaultCommand, defaultCommandIndex },
                { FrameNavigationAdapter.MessageDialogParameterCancelCommand, cancelCommandIndex },
                { FrameNavigationAdapter.MessageDialogParameterCommands, commands }
            };

            var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(FrameNavigationAdapter.MessageDialogUri, UriKind.Relative), data), typeof(UICommand)));
            return new NavigationResult<UICommand>(result.Request, result.NavigationTask, result.CancellationSource, result.Response.ContinueWith(x => (UICommand)x.Result));
        }
    }
}
