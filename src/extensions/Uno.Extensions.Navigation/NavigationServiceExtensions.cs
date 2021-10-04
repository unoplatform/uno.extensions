using System;
using System.Collections.Generic;
using System.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;

namespace Uno.Extensions.Navigation;

public static class NavigationServiceExtensions
{
    public static NavigationResponse NavigateByPathAsync(this INavigationService service, object sender, string path, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(path, UriKind.Relative), data), cancellation));
    }

    public static NavigationResponse<TResult> NavigateByPathAsync<TResult>(this INavigationService service, object sender, string path, object data = null, CancellationToken cancellation = default)
    {
        var result = service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(path, UriKind.Relative), data), cancellation, typeof(TResult)));
        return new NavigationResponse<TResult>(result);
    }

    public static NavigationResponse NavigateToViewAsync<TView>(this INavigationService service, object sender, string relativePathModifier = RouteConstants.RelativePath.Current, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateToViewAsync(sender, typeof(TView), relativePathModifier, data, cancellation);
    }

    public static NavigationResponse NavigateToViewAsync(this INavigationService service, object sender, Type viewType, string relativePathModifier = RouteConstants.RelativePath.Current, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.LookupByView(viewType);
        return service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), cancellation));
    }

    public static NavigationResponse<TResult> NavigateToViewAsync<TView, TResult>(this INavigationService service, object sender, string relativePathModifier = RouteConstants.RelativePath.Current, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.LookupByView(typeof(TView));
        var result = service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), cancellation, typeof(TResult)));
        return new NavigationResponse<TResult>(result);
    }

    public static NavigationResponse NavigateToViewModelAsync<TViewViewModel>(this INavigationService service, object sender, string relativePathModifier = RouteConstants.RelativePath.Current, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateToViewModelAsync(sender, typeof(TViewViewModel), relativePathModifier, data, cancellation);
    }

    public static NavigationResponse NavigateToViewModelAsync(this INavigationService service, object sender, Type viewModelType, string relativePathModifier = RouteConstants.RelativePath.Current, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.LookupByViewModel(viewModelType);
        return service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), cancellation));
    }

    public static NavigationResponse<TResult> NavigateToViewModelAsync<TViewViewModel, TResult>(this INavigationService service, object sender, string relativePathModifier = RouteConstants.RelativePath.Current, object data = null, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.LookupByViewModel(typeof(TViewViewModel));
        var result = service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), cancellation, typeof(TResult)));
        return new NavigationResponse<TResult>(result);
    }

    public static NavigationResponse NavigateForDataAsync<TData>(this INavigationService service, object sender, TData data, string relativePathModifier = RouteConstants.RelativePath.Current, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.LookupByData(typeof(TData));
        return service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), cancellation));
    }

    public static NavigationResponse NavigateForResultDataAsync<TResultData>(this INavigationService service, object sender, string relativePathModifier = RouteConstants.RelativePath.Current, CancellationToken cancellation = default)
    {
        var mapping = Ioc.Default.GetRequiredService<IRouteMappings>();
        var map = mapping.LookupByResultData(typeof(TResultData));
        return service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(map.FullPath(relativePathModifier), UriKind.Relative)), cancellation,typeof(TResultData)));
    }

    public static NavigationResponse NavigateToPreviousViewAsync(this INavigationService service, object sender, string relativePathModifier = RouteConstants.RelativePath.Current, object data = null, CancellationToken cancellation = default)
    {
        return service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(RouteMap.CombinePathWithRelativePath(RouteConstants.PreviousViewUri, relativePathModifier), UriKind.Relative), data), cancellation));
    }

    public static NavigationResponse<Windows.UI.Popups.UICommand> ShowMessageDialogAsync(
        this INavigationService service,
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

        var result = service.NavigateAsync(new NavigationRequest(sender, new Route(new Uri(RouteConstants.MessageDialogUri, UriKind.Relative), data), cancellation, typeof(UICommand)));
        return new NavigationResponse<UICommand>(result);
    }
}
