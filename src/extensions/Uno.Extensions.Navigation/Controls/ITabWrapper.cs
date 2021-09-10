using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Windows.Foundation;
using System.Threading;
using Uno.Extensions.Navigation.Adapters;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
using Microsoft.UI.Xaml.Controls;
#endif


namespace Uno.Extensions.Navigation.Controls;

public interface ITabWrapper : IControlNavigation
{
    string CurrentTabName { get; }

    bool ContainsTab(string tabName);
}

public record Dialog(IDialogManager Manager, IAsyncInfo ShowTask, NavigationContext Context) { }

public interface IDialogManager
{
    object CloseDialog(Dialog dialog, NavigationContext context, object responseData);
    Dialog DisplayDialog(INavigationService navigation, NavigationContext context, object vm);
}

public interface IDialogProvider
{
    Dialog CreateDialog(INavigationService navigation, NavigationContext context, object vm);
}

public record DialogProvider(IEnumerable<IDialogManager> Dialogs) : IDialogProvider
{
    public Dialog CreateDialog(INavigationService navigation, NavigationContext context, object vm)
    {
        foreach (var dlg in Dialogs)
        {
            var dialog = dlg.DisplayDialog(navigation, context, vm);
            if (dialog is not null)
            {
                return dialog;
            }
        }
        return null;
    }

}

public class NavigationContentDialog : IDialogManager
{
    public object CloseDialog(Dialog dialog, NavigationContext context, object responseData)
    {

        if (!(responseData is ContentDialogResult))
        {
            dialog.ShowTask.Cancel();
        }

        var resultType = dialog.Context.Request.Result;

        if (resultType is not null && responseData is not null)
        {
            if (resultType == typeof(ContentDialogResult))
            {
                if (responseData is not ContentDialogResult)
                {
                    responseData = ContentDialogResult.None;
                }
            }
            else if (resultType == typeof(ContentResult))
            {
                if (responseData is ContentDialogResult result)
                {
                    responseData = new ContentResult(result);
                }
                else
                {
                    responseData = new ContentResult(ContentDialogResult.None, responseData);
                }
            }
        }

        return responseData;
    }

    public Dialog DisplayDialog(INavigationService navigation, NavigationContext context, object vm)
    {
        if (!(context.Mapping?.View?.IsSubclassOf(typeof(ContentDialog)) ?? false))
        {
            return null;
        }

        var dialog = Activator.CreateInstance(context.Mapping.View) as ContentDialog;
        if (vm is not null)
        {
            dialog.DataContext = vm;
        }
        if (dialog is INavigationAware navAware)
        {
            navAware.Navigation = navigation;
        }

        var showTask = dialog.ShowAsync();
        showTask.AsTask().ContinueWith(result =>
        {
            if (result.Status != TaskStatus.Canceled &&
            context.ResultCompletion.Task.Status != TaskStatus.Canceled &&
            context.ResultCompletion.Task.Status != TaskStatus.RanToCompletion)
            {
                navigation.Navigate(new NavigationRequest(dialog, new NavigationRoute(new Uri(NavigationConstants.PreviousViewUri, UriKind.Relative), result.Result)));
            }
        }, CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                        TaskScheduler.FromCurrentSynchronizationContext());
        return new Dialog(this, showTask, context);
    }
}

public class NavigationMessageDialog : IDialogManager
{
    public object CloseDialog(Dialog dialog, NavigationContext context, object responseData)
    {
        dialog.ShowTask.Cancel();
        return responseData;
    }

    public Dialog DisplayDialog(INavigationService navigation, NavigationContext context, object vm)
    {
        if (context.Path != NavigationConstants.MessageDialogUri)
        {
            return null;
        }

            var data = context.Data;
        var md = new MessageDialog(data[NavigationConstants.MessageDialogParameterContent] as string, data[NavigationConstants.MessageDialogParameterTitle] as string)
        {
            Options = (MessageDialogOptions)data[NavigationConstants.MessageDialogParameterOptions],
            DefaultCommandIndex = (uint)data[NavigationConstants.MessageDialogParameterDefaultCommand],
            CancelCommandIndex = (uint)data[NavigationConstants.MessageDialogParameterCancelCommand]
        };
        md.Commands.AddRange((data[NavigationConstants.MessageDialogParameterCommands] as UICommand[]) ?? new UICommand[] { });
        var showTask = md.ShowAsync();
        showTask.AsTask().ContinueWith(result =>
        {
            if (result.Status != TaskStatus.Canceled &&
            context.ResultCompletion.Task.Status != TaskStatus.Canceled &&
            context.ResultCompletion.Task.Status != TaskStatus.RanToCompletion)
            {
                navigation.Navigate(new NavigationRequest(md, new NavigationRoute(new Uri(NavigationConstants.PreviousViewUri, UriKind.Relative), result.Result)));
            }
        }, CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                        TaskScheduler.FromCurrentSynchronizationContext());
        return new Dialog(this, showTask, context);
    }
}
public record ContentResult(ContentDialogResult Result, object Data = null)
{
    public static implicit operator ContentDialogResult(
                                   ContentResult entity)
    {
        return entity.Result;
    }
}
