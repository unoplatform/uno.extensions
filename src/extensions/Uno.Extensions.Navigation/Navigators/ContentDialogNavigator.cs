using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Windows.Foundation;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public class ContentDialogNavigator : DialogNavigator
{
    public ContentDialogNavigator(
        ILogger<ContentDialogNavigator> logger,
        IRouteMappings mappings,
        IRegion region)
        : base(logger, mappings, region)
    {
    }

    protected override IAsyncInfo DisplayDialog(Route route, object viewModel)
    {
        var navigation = Region.Navigator();
        var services = this.Get<IServiceProvider>();
        var mapping = Mappings.Find(route);
        var dialog = Activator.CreateInstance(mapping?.View) as ContentDialog;

        dialog.InjectServicesAndSetDataContext(services, navigation, viewModel);

        var showTask = dialog.ShowAsync();
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
