using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation.Navigators;

public class ContentDialogNavigator : DialogNavigator
{
    public ContentDialogNavigator(
        ILogger<ContentDialogNavigator> logger,
        IRouteResolver routeResolver,
        IRegion region)
        : base(logger, routeResolver, region)
    {
    }

    protected override IAsyncInfo? DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel)
    {
        var route = request.Route;
        var navigation = Region.Navigator();
        var services = this.Get<IServiceProvider>();
        var mapping = RouteResolver.Find(route);
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
						navigation.NavigatePreviousWithResultAsync(Option.Some(result.Result));
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                TaskScheduler.FromCurrentSynchronizationContext());
        return showTask;
    }
}
