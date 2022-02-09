using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;

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

	protected override async Task<IAsyncInfo?> DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel)
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
		if (dialog is null)
		{
			return null;
		}

		dialog.SetInstance(Region);

		dialog.InjectServicesAndSetDataContext(services, navigation, viewModel);

		var showTask = dialog.ShowAsync();
		showTask.AsTask()
			.ContinueWith(result =>
				{
					if (result.Status != TaskStatus.Canceled)
					{
						navigation.NavigatePreviousWithResultAsync(request.Sender, data: Option.Some(result.Result));
					}
				},
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
				TaskScheduler.FromCurrentSynchronizationContext());

		await dialog.EnsureLoaded();

		if (dialog.Content is FrameworkElement dialogElement)
		{
			dialogElement.SetName(route.Base);
			dialogElement.ReassignRegionParent();
		}


		if (request.Cancellation.HasValue && request.Cancellation.CanBeCanceled)
		{
			request.Cancellation.Value.Register(() =>
			{
				showTask.Cancel();
			});
		}

		return showTask;
	}
}
