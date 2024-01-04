namespace Uno.Extensions.Navigation.Navigators;

public abstract class DialogNavigator : ClosableNavigator
{
	public override bool CanGoBack => ShowTask is not null;

	private IAsyncInfo? ShowTask { get; set; }

	protected Window Window { get; }

	protected DialogNavigator(
		ILogger<DialogNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		Window window)
		: base(logger, dispatcher, region, resolver)
	{
		Window = window;
	}

	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		// Capture the Source - return value can be ignored
		_ = await base.ExecuteRequestAsync(request);

		var route = request.Route;

		// Make sure any existing dialogs are closed
		// This is true whether we're attempting to forward navigate (ie new dialog)
		// or back (ie close dialog)
		CloseDialog();

		// If this is back navigation, then make sure it's used to close
		// any of the open dialogs
		if (!route.FrameIsBackNavigation())
		{
			var mapping = Resolver.FindByPath(route.Base);
			var viewModel = (Region.Services is not null && mapping?.ViewModel is not null) ? await CreateViewModel(Region.Services, request, route, mapping) : default(object);
			ShowTask = await DisplayDialog(request, mapping?.RenderView, viewModel);
		}

		if (request.Cancellation.HasValue &&
			request.Cancellation.Value.CanBeCanceled)
		{
			request.Cancellation.Value.Register(async () =>
			{
				await this.Dispatcher.ExecuteAsync(() => CloseDialog());
			});
		}

		var responseRequest = route with { Path = null };
		return responseRequest;
	}

	protected void CloseDialog()
	{
		var dialog = ShowTask;
		ShowTask = null;

		dialog?.Cancel();
	}

	protected abstract Task<IAsyncInfo?> DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel);
	protected override async Task CloseNavigator() => CloseDialog();

}
