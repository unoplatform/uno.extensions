using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;

namespace Uno.Extensions.Navigation;

public static class NavigationServiceExtensions
{
    public static NavigationResponse NavigateByPath(this INavigationService service, object sender, string path, object data = null)
    {
        return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(path, UriKind.Relative), data)));
    }

    public static NavigationResponse<TResult> NavigateByPath<TResult>(this INavigationService service, object sender, string path, object data = null)
    {
        var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(path, UriKind.Relative), data), typeof(TResult)));
        return new NavigationResponse<TResult>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (TResult)x.Result));
    }

    public static NavigationResponse NavigateToView<TView>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        return service.NavigateToView(sender, typeof(TView), relativePathModifier, data);
    }

    public static NavigationResponse NavigateToView(this INavigationService service, object sender, Type viewType, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByView(viewType);
        return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse<TResult> NavigateToView<TView, TResult>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByView(typeof(TView));
        var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), typeof(TResult)));
        return new NavigationResponse<TResult>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (TResult)x.Result));
    }

    public static NavigationResponse NavigateToViewModel<TViewViewModel>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        return service.NavigateToViewModel(sender, typeof(TViewViewModel), relativePathModifier, data);
    }

    public static NavigationResponse NavigateToViewModel(this INavigationService service, object sender, Type viewModelType, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByViewModel(viewModelType);
        return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse<TResult> NavigateToViewModel<TViewViewModel, TResult>(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByViewModel(typeof(TViewViewModel));
        var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data), typeof(TResult)));
        return new NavigationResponse<TResult>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (TResult)x.Result));
    }

    public static NavigationResponse NavigateForData<TData>(this INavigationService service, object sender, TData data, string relativePathModifier = NavigationConstants.RelativePath.Current)
    {
        var mapping = Ioc.Default.GetRequiredService<INavigationMapping>();
        var map = mapping.LookupByData(typeof(TData));
        return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(map.FullPath(relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse NavigateToPreviousView(this INavigationService service, object sender, string relativePathModifier = NavigationConstants.RelativePath.Current, object data = null)
    {
        return service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(NavigationMap.CombinePathWithRelativePath(NavigationConstants.PreviousViewUri, relativePathModifier), UriKind.Relative), data)));
    }

    public static NavigationResponse<Windows.UI.Popups.UICommand> ShowMessageDialog(
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

        var result = service.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(NavigationConstants.MessageDialogUri, UriKind.Relative), data), typeof(UICommand)));
        return new NavigationResponse<UICommand>(result.Request, result.NavigationTask, result.CancellationSource, result.Result.ContinueWith(x => (UICommand)x.Result));
    }
}
