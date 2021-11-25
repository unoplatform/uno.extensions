using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Windows.Foundation;
#if !WINUI
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

    protected override IAsyncInfo? DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel)
    {
        var route = request.Route;
        var navigation = Region.Navigator();
        var services = this.Get<IServiceProvider>();
        var mapping = Mappings.Find(route);
        if (
            navigation is null ||
            services is null ||
            mapping?.View is null)
        {
            return null;
        }

        var dialog = Activator.CreateInstance(mapping.View) as ContentDialog;
        if(dialog is null)
        {
            return null;
        }

        dialog.InjectServicesAndSetDataContext(services, navigation, viewModel);

        var showTask = dialog.ShowAsync();
        showTask.AsTask()
            .ContinueWith(result =>
                {
                    if (result.Status != TaskStatus.Canceled)
                    {
						navigation.NavigatePreviousWithResultAsync(Options.Option.Some(result.Result));
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                TaskScheduler.FromCurrentSynchronizationContext());
        return showTask;
    }
}
