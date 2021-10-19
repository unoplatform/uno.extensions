using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.UI.Popups;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation;

public static class NavigatorExtensions
{
    public static Task<NavigationResponse> NavigateByPathAsync(this INavigator service, object sender, string path, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateAsync(new NavigationRequest(sender, new Uri(RouteMap.CombinePathWithRelativePath(path, relativePathModifier), UriKind.Relative).AsRoute(data), cancellation));
    }

    public static async Task<NavigationResponse<TResult>> NavigateByPathAsync<TResult>(this INavigator service, object sender, string path, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        var result = await service.NavigateAsync(new NavigationRequest(sender, new Uri(RouteMap.CombinePathWithRelativePath(path, relativePathModifier), UriKind.Relative).AsRoute(data), cancellation, typeof(TResult)));
        return result.As<TResult>();
    }

    public static Task<NavigationResponse> NavigateToViewAsync<TView>(this INavigator service, object sender, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateToViewAsync(sender, typeof(TView), relativePathModifier, data, cancellation);
    }

    public static Task<NavigationResponse> NavigateToViewAsync(this INavigator service, object sender, Type viewType, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.FindByView(viewType);
        return service.NavigateAsync(new NavigationRequest(sender, new Uri(map.FullPath(relativePathModifier), UriKind.Relative).AsRoute(data), cancellation));
    }

    public static async Task<NavigationResponse<TResult>> NavigateToViewAsync<TView, TResult>(this INavigator service, object sender, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.FindByView(typeof(TView));
        var result = await service.NavigateAsync(new NavigationRequest(sender, new Uri(map.FullPath(relativePathModifier), UriKind.Relative).AsRoute(data), cancellation, typeof(TResult)));
        return result.As<TResult>();
    }

    public static Task<NavigationResponse> NavigateToViewModelAsync<TViewViewModel>(this INavigator service, object sender, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateToViewModelAsync(sender, typeof(TViewViewModel), relativePathModifier, data, cancellation);
    }

    public static Task<NavigationResponse> NavigateToViewModelAsync(this INavigator service, object sender, Type viewModelType, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.FindByViewModel(viewModelType);
        return service.NavigateAsync(new NavigationRequest(sender, new Uri(map.FullPath(relativePathModifier), UriKind.Relative).AsRoute(data), cancellation));
    }

    public static async Task<NavigationResponse<TResult>> NavigateToViewModelAsync<TViewViewModel, TResult>(this INavigator service, object sender, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.FindByViewModel(typeof(TViewViewModel));
        var result = await service.NavigateAsync(new NavigationRequest(sender, new Uri(map.FullPath(relativePathModifier), UriKind.Relative).AsRoute(data), cancellation, typeof(TResult)));
        return result.As<TResult>();
    }

    public static Task<NavigationResponse> NavigateForDataAsync<TData>(this INavigator service, object sender, TData data, string relativePathModifier = Schemes.Parent, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.FindByData(typeof(TData));
        return service.NavigateAsync(new NavigationRequest(sender, new Uri(map.FullPath(relativePathModifier), UriKind.Relative).AsRoute(data), cancellation));
    }

    public static Task<NavigationResponse> NavigateForResultDataAsync<TResultData>(this INavigator service, object sender, string relativePathModifier = Schemes.Parent, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.FindByResultData(typeof(TResultData));
        return service.NavigateAsync(new NavigationRequest(sender, new Uri(map.FullPath(relativePathModifier), UriKind.Relative).AsRoute(), cancellation, typeof(TResultData)));
    }

    public static Task<NavigationResponse> NavigateToPreviousViewAsync(this INavigator service, object sender, string relativePathModifier = Schemes.Parent, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateAsync(new NavigationRequest(sender, new Uri(RouteMap.CombinePathWithRelativePath(Schemes.NavigateBack + string.Empty, relativePathModifier), UriKind.Relative).AsRoute(data), cancellation));
    }

    public static async Task<NavigationResponse<Windows.UI.Popups.UICommand>> ShowMessageDialogAsync(
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

        var result = await service.NavigateAsync(new NavigationRequest(sender, new Uri(Schemes.Dialog + typeof(MessageDialog).Name, UriKind.Relative).AsRoute(data), cancellation, typeof(UICommand)));
        return result.As<UICommand>();
    }

#if __IOS__
    public static async Task<NavigationResponse<TSource>> ShowPickerAsync<TSource>(
       this INavigationService service,
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

        var result = await service.NavigateAsync(new NavigationRequest(sender, new Uri(Schemes.Dialog + typeof(Picker).Name, UriKind.Relative).BuildRoute(data), cancellation, typeof(TSource)));
        return NavigationResponse<TSource>.FromResponse(result);
    }
#endif
}
