using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.ViewModels;
using Windows.Foundation;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#endif

namespace Uno.Extensions.Navigation.Services;

public class MessageDialogNavigator : DialogNavigator
{
    public MessageDialogNavigator(
        ILogger<DialogNavigator> logger,
        IRegion region)
        : base(logger, region)
    {
    }

    protected override IAsyncInfo DisplayDialog(NavigationContext context, object vm)
    {
        var navigation = context.Navigation;

        var data = context.Request.Route.Data;
        var md = new MessageDialog(data[RouteConstants.MessageDialogParameterContent] as string, data[RouteConstants.MessageDialogParameterTitle] as string)
        {
            Options = (MessageDialogOptions)data[RouteConstants.MessageDialogParameterOptions],
            DefaultCommandIndex = (uint)data[RouteConstants.MessageDialogParameterDefaultCommand],
            CancelCommandIndex = (uint)data[RouteConstants.MessageDialogParameterCancelCommand]
        };
        md.Commands.AddRange(data[RouteConstants.MessageDialogParameterCommands] as UICommand[] ?? new UICommand[] { });
        var showTask = md.ShowAsync();
        showTask.AsTask()
            .ContinueWith(result =>
                {
                    if (result.Status != TaskStatus.Canceled)
                    {
                        var responseNav = navigation as ResponseNavigator;
                        if (responseNav is not null &&
                            responseNav.ResultCompletion.Task.Status != TaskStatus.Canceled &&
                            responseNav.ResultCompletion.Task.Status != TaskStatus.RanToCompletion)
                        {
                            responseNav.ResultCompletion.TrySetResult(Options.Option.Some(result.Result));
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                TaskScheduler.FromCurrentSynchronizationContext());
        return showTask;
    }
}
