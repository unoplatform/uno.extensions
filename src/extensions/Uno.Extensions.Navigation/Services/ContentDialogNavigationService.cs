using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.ViewModels;
using Windows.Foundation;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#endif

namespace Uno.Extensions.Navigation.Services;

public class ContentDialogNavigationService : DialogNavigationService
{
    public ContentDialogNavigationService(
        ILogger<ContentDialogNavigationService> logger,
        IRegion region,
        IRegionNavigationServiceFactory serviceFactory,
        IServiceProvider scopedServices)
        : base(logger, region, serviceFactory, scopedServices)
    {
    }

    protected override IAsyncInfo DisplayDialog(NavigationContext context, object vm)
    {
        var navigation = context.Navigation;
        var dialog = Activator.CreateInstance(context.Mapping.View) as ContentDialog;

        dialog.InjectServicesAndSetDataContext(context.Services, context.Navigation, vm);

        var showTask = dialog.ShowAsync();
        showTask.AsTask()
            .ContinueWith(result =>
                {
                    if (result.Status != TaskStatus.Canceled)
                    {
                        var responseNav = navigation as ResponseNavigationService;
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
