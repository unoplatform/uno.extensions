using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#endif

namespace Uno.Extensions.Navigation.Regions;

public class ContentDialogRegion : DialogRegion
{
    public ContentDialogRegion(
        ILogger<ContentDialogRegion> logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager) : base(logger, scopedServices, navigation, viewModelManager)
    {
    }

    protected override object CloseDialog(Dialog dialog, NavigationContext context, object responseData)
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

    protected override Dialog DisplayDialog(NavigationContext context, object vm)
    {
        var navigation = context.Navigation;
        var dialog = Activator.CreateInstance(context.Mapping.View) as ContentDialog;
        if (vm is not null)
        {
            dialog.DataContext = vm;
        }

        if (dialog is IInjectable<INavigationService> navAware)
        {
            navAware.Inject(navigation);
        }

        if (dialog is IInjectable<IServiceProvider> spAware)
        {
            spAware.Inject(context.Services);
        }

        var showTask = dialog.ShowAsync();
        showTask.AsTask().ContinueWith(result =>
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
        }, CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                        TaskScheduler.FromCurrentSynchronizationContext());
        return new Dialog(showTask, context);
    }
}
