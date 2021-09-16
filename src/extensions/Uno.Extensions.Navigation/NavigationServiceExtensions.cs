using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;

namespace Uno.Extensions.Navigation;

public static class NavigationServiceExtensions
{
    public static NavigationResponse NavigateByPathAsync(this INavigationService service, object sender, string path, object data = null)
    {
        return service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(path, UriKind.Relative), data)));
    }

    public static NavigationResponse<TResult> NavigateByPathAsync<TResult>(this INavigationService service, object sender, string path, object data = null)
    {
        var result = service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(path, UriKind.Relative), data), typeof(TResult)));
        return new NavigationResponse<TResult>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (TResult)x.Result));
    }

    public static NavigationResponse NavigateToViewAsync<TView>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        return service.NavigateToViewAsync(sender, typeof(TView), relativePathModifier, data);
    }

    public static NavigationResponse NavigateToViewAsync(this INavigationService service, object sender, Type viewType, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByView(viewType);
        return service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse<TResult> NavigateToViewAsync<TView, TResult>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByView(typeof(TView));
        var result = service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), typeof(TResult)));
        return new NavigationResponse<TResult>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (TResult)x.Result));
    }

    public static NavigationResponse NavigateToViewModelAsync<TViewViewModel>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        return service.NavigateToViewModelAsync(sender, typeof(TViewViewModel), relativePathModifier, data);
    }

    public static NavigationResponse NavigateToViewModelAsync(this INavigationService service, object sender, Type viewModelType, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByViewModel(viewModelType);
        return service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse<TResult> NavigateToViewModelAsync<TViewViewModel, TResult>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByViewModel(typeof(TViewViewModel));
        var result = service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), typeof(TResult)));
        return new NavigationResponse<TResult>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (TResult)x.Result));
    }

    public static NavigationResponse NavigateForDataAsync<TData>(this INavigationService service, object sender, TData data, string relativePathModifier = NavigationConstants.RelativePath.Current)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByData(typeof(TData));
        return service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse NavigateToPreviousViewAsync(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        return service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(NavigationMap.CombinePathWithRelativePath(NavigationConstants.PreviousViewUri, relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse<Windows.UI.Popups.UICommand> ShowMessageDialogAsync(
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
                { NavigationConstants.MessageDialogParameterTitle, title },
                { NavigationConstants.MessageDialogParameterContent, content },
                { NavigationConstants.MessageDialogParameterOptions, options },
                { NavigationConstants.MessageDialogParameterDefaultCommand, defaultCommandIndex },
                { NavigationConstants.MessageDialogParameterCancelCommand, cancelCommandIndex },
                { NavigationConstants.MessageDialogParameterCommands, commands }
            };

        var result = service.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(NavigationConstants.MessageDialogUri, UriKind.Relative), data), typeof(UICommand)));
        return new NavigationResponse<UICommand>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (UICommand)x.Result));
    }
}
