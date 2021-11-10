using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Windows.Foundation;
#if !WINUI
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public class MessageDialogNavigator : DialogNavigator
{
    public MessageDialogNavigator(
        ILogger<DialogNavigator> logger,
        IRouteMappings mappings,
        IRegion region)
        : base(logger, mappings, region)
    {
    }

    protected override IAsyncInfo? DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel)
    {
        var route = request.Route;
        var navigation = Region.Navigator();
        var services = this.Get<IServiceProvider>();
        var mapping = Mappings.Find(route);

        var data = route.Data;
        if(data is null)
        {
            return null;
        }

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
