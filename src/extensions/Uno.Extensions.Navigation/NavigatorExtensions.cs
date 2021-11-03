using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
using System.Linq;
using Uno.Extensions.Navigation.Navigators;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using UICommand = Windows.UI.Popups.UICommand;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
#endif

namespace Uno.Extensions.Navigation;

public static class NavigatorExtensions
{
    internal static IRouteMappings GetMapping(this INavigator navigator)
    {
        return navigator.Get<IServiceProvider>().GetService<IRouteMappings>();
    }

    /// <summary>
    /// Navigates to the specified route
    /// </summary>
    /// <param name="service">The Navigator</param>
    /// <param name="sender">The sender of the navigation request</param>
    /// <param name="route">The route to navigate to</param>
    /// <param name="scheme">A scheme to appeand to the request (eg ../ to direct request to parent region)</param>
    /// <param name="data">Data object to be passed with navigation</param>
    /// <param name="cancellation">Cancellation token to allow for cancellation of navigation</param>
    /// <returns>NavigationResponse that indicates success</returns>
    public static Task<NavigationResponse> NavigateToRouteAsync(
        this INavigator service, object sender, string route, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateAsync(route.WithScheme(scheme).AsRequest(sender, data, cancellation));
    }

    /// <summary>
    /// Navigates to the specified route
    /// </summary>
    /// <typeparam name="TResult">The type of data that's expected to be returned</typeparam>
    /// <param name="service">The Navigator</param>
    /// <param name="sender">The sender of the navigation request</param>
    /// <param name="route">The route to navigate to</param>
    /// <param name="scheme">A scheme to appeand to the request (eg ../ to direct request to parent region)</param>
    /// <param name="data">Data object to be passed with navigation</param>
    /// <param name="cancellation">Cancellation token to allow for cancellation of navigation</param>
    /// <returns>NavigationResultResponse that indicates success and contains an awaitable result</returns>
    public static async Task<NavigationResultResponse<TResult>> NavigateToRouteForResultAsync<TResult>(
        this INavigator service, object sender, string route, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        var result = await service.NavigateAsync(route.WithScheme(scheme).AsRequest<TResult>(sender, data, cancellation));
        return result.As<TResult>();
    }

    public static Task<NavigationResponse> NavigateToViewAsync<TView>(
        this INavigator service, object sender, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateToViewAsync(sender, typeof(TView), scheme, data, cancellation);
    }

    public static Task<NavigationResponse> NavigateToViewAsync(
        this INavigator service, object sender, Type viewType, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        var mappings = service.GetMapping();
        var map = mappings.FindByView(viewType);
        return service.NavigateAsync(map.Path.WithScheme(scheme).AsRequest(sender, data, cancellation));
    }

    public static async Task<NavigationResultResponse<TResult>> NavigateToViewForResultAsync<TView, TResult>(
        this INavigator service, object sender, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        var mappings = service.GetMapping();
        var map = mappings.FindByView(typeof(TView));
        var result = await service.NavigateAsync(map.Path.WithScheme(scheme).AsRequest<TResult>(sender, data, cancellation));
        return result.As<TResult>();
    }

    public static Task<NavigationResponse> NavigateToViewModelAsync<TViewViewModel>(
        this INavigator service, object sender, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateToViewModelAsync(sender, typeof(TViewViewModel), scheme, data, cancellation);
    }

    public static Task<NavigationResponse> NavigateToViewModelAsync(
        this INavigator service, object sender, Type viewModelType, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        var mappings = service.GetMapping();
        var map = mappings.FindByViewModel(viewModelType);
        return service.NavigateAsync(map.Path.WithScheme(scheme).AsRequest(sender, data, cancellation));
    }

    public static async Task<NavigationResultResponse<TResult>> NavigateToViewModelForResultAsync<TViewViewModel, TResult>(
        this INavigator service, object sender, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        var mappings = service.GetMapping();
        var map = mappings.FindByViewModel(typeof(TViewViewModel));
        var result = await service.NavigateAsync(map.Path.WithScheme(scheme).AsRequest<TResult>(sender, data, cancellation));
        return result.As<TResult>();
    }

    public static Task<NavigationResponse> NavigateToDataAsync<TData>(
        this INavigator service, object sender, TData data, string scheme = Schemes.None, CancellationToken cancellation = default)
    {
        var mappings = service.GetMapping();
        var map = mappings.FindByData(typeof(TData));
        return service.NavigateAsync(map.Path.WithScheme(scheme).AsRequest(sender, data, cancellation));
    }

    public static async Task<NavigationResultResponse<TResultData>> NavigateForResultAsync<TResultData>(
        this INavigator service, object sender, string scheme = Schemes.None, object data = null, CancellationToken cancellation = default)
    {
        var mappings = service.GetMapping();
        var map = mappings.FindByResultData(typeof(TResultData));
        var result = await service.NavigateAsync(map.Path.WithScheme(scheme).AsRequest<TResultData>(sender, data, cancellation));
        return result.As<TResultData>();
    }

    public static Task<NavigationResponse> NavigateToPreviousViewAsync(
        this INavigator service, object sender, string scheme = Schemes.None, CancellationToken cancellation = default)
    {
        return service.NavigateAsync((Schemes.NavigateBack + string.Empty).WithScheme(scheme).AsRequest(sender, cancellationToken: cancellation));
    }

    public static Task<NavigationResponse> NavigateToPreviousViewAsync<TResult>(
    this INavigator service, object sender, string scheme = Schemes.None, Options.Option<TResult> data = null, CancellationToken cancellation = default)
    {
        return service.NavigateAsync((Schemes.NavigateBack + string.Empty).WithScheme(scheme).AsRequest(sender, data, cancellation));
    }

    public static async Task<NavigationResultResponse<Windows.UI.Popups.UICommand>> ShowMessageDialogAsync(
        this INavigator service,
        object sender,
        string content,
        string title = null,
        MessageDialogOptions options = MessageDialogOptions.None,
        uint defaultCommandIndex = 0,
        uint cancelCommandIndex = 0,
        Windows.UI.Popups.UICommand[] commands = null,
        CancellationToken cancellation = default)
    {
        var data = new Dictionary<string, object>()
            {
                { RouteConstants.MessageDialogParameterTitle, title },
                { RouteConstants.MessageDialogParameterContent, content },
                { RouteConstants.MessageDialogParameterOptions, options },
                { RouteConstants.MessageDialogParameterDefaultCommand, defaultCommandIndex },
                { RouteConstants.MessageDialogParameterCancelCommand, cancelCommandIndex },
                { RouteConstants.MessageDialogParameterCommands, commands }
            };

        var result = await service.NavigateAsync((Schemes.Dialog + typeof(MessageDialog).Name).AsRequest<UICommand>(sender,data, cancellation));
        return result.As<UICommand>();
    }

#if __IOS__
    public static async Task<NavigationResultResponse<TSource>> ShowPickerAsync<TSource>(
       this INavigator service,
       object sender,
       IEnumerable<TSource> itemsSource,
       object itemTemplate = null,
       CancellationToken cancellation = default)
    {
        var data = new Dictionary<string, object>()
            {
                { RouteConstants.PickerItemsSource, itemsSource },
                { RouteConstants.PickerItemTemplate, itemTemplate }
            };

        var result = await service.NavigateAsync((Schemes.Dialog + typeof(Picker).Name).AsRequest(sender,data, cancellation, typeof(TSource)));
        return result.As<TSource>();
    }
#endif

    public static Task<NavigationResponse> GoBack(this INavigator navigator, object sender)
    {
        var region = navigator.Get<IServiceProvider>().GetService<IRegion>();
        region = region.Root();
        var gobackNavigator = region.FindChildren(
            child=>child.Services.GetService<INavigator>() is ControlNavigator controlNavigator &&
                controlNavigator.CanGoBack).Last()?.Navigator();
        return gobackNavigator?.NavigateToPreviousViewAsync(sender);
    }

}
