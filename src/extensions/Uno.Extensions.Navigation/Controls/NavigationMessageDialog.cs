using System;
using System.Threading.Tasks;
using System.Threading;
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
