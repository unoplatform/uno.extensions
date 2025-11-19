namespace Uno.Extensions.Navigation;

public class ResponseNavigator<TResult> : IResponseNavigator, IInstance<IServiceProvider>
{
	private INavigator Navigation { get; }

	private TaskCompletionSource<Option<TResult>> ResultCompletion { get; }

	public Route? Route => Navigation.Route;

	private IDispatcher Dispatcher => (Navigation as Navigator)!.Dispatcher;

	public IServiceProvider? Instance => Navigation.Get<IServiceProvider>();

	private SystemNavigationManager? _systemNavigationManager;

	public ResponseNavigator(INavigator internalNavigation, NavigationRequest request)
	{
		Navigation = internalNavigation;
		ResultCompletion = new TaskCompletionSource<Option<TResult>>();


		if (request.Cancellation.HasValue)
		{
			request.Cancellation.Value.Register(async () =>
			{
				await ApplyResult(Option.None<TResult>());
			});
		}

		// Hook up to SystemNavigationManager.BackRequested to handle back navigation
		// from NavigationBar (Toolkit) and other sources that raise this event
		_systemNavigationManager = SystemNavigationManager.GetForCurrentView();
		if (_systemNavigationManager != null)
		{
			_systemNavigationManager.BackRequested += OnSystemBackRequested;
		}

		// Replace the navigator
		Navigation.Get<IServiceProvider>()?.AddScopedInstance<INavigator>(this);
	}

	public async Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
	{
		var navResponse = await Navigation.NavigateAsync(request);

		if (request.Route.FrameIsBackNavigation() ||
			// Note: Disabling parent routing - leaving this code in case parent routing is required
			//request.Route.TrimQualifier(Qualifiers.Parent).FrameIsBackNavigation() || // Handles ../- 
			request.Route.TrimQualifier(Qualifiers.Nested).FrameIsBackNavigation() || // Handles ./- 
			(request.Route.IsRoot() && request.Route.TrimQualifier(Qualifiers.Root).FrameIsBackNavigation() && this.Navigation.GetParent() == null))
		{
			var responseData = request.Route.ResponseData();
			var result = responseData is Option<TResult> res ? res : default;
			if (result.Type != OptionType.Some)
			{
				if (responseData is IOption objectResponse)
				{
					responseData = objectResponse.SomeOrDefault();
				}

				if (responseData is TResult data)
				{
					result = Option.Some(data);
				}
				else
				{
					result = Option.None<TResult>();
				}
			}
			await ApplyResult(result);
		}

		return navResponse;
	}

	private async void OnSystemBackRequested(object? sender, BackRequestedEventArgs e)
	{
		// When back navigation is requested via SystemNavigationManager (e.g., from NavigationBar),
		// complete the ForResult task with None to prevent the race condition
		// Note: We don't mark e.Handled here because BackButtonService will handle the actual navigation
		await ApplyResult(Option.None<TResult>());
	}

	private async Task ApplyResult(Option<TResult> responseData)
	{
		if (ResultCompletion.Task.Status == TaskStatus.Canceled ||
			ResultCompletion.Task.Status == TaskStatus.RanToCompletion)
		{
			return;
		}

		// Unhook from SystemNavigationManager to avoid memory leaks
		if (_systemNavigationManager != null)
		{
			_systemNavigationManager.BackRequested -= OnSystemBackRequested;
			_systemNavigationManager = null;
		}

		// Restore the navigator
		Navigation.Get<IServiceProvider>()?.AddScopedInstance<INavigator>(this.Navigation);

		await Dispatcher.ExecuteAsync(() =>
		{
			ResultCompletion.TrySetResult(responseData);
		});
	}

	public NavigationResponse AsResponseWithResult(NavigationResponse? response)
	{
		if (response is NavigationResultResponse<TResult> navResponse)
		{
			navResponse.Result.ContinueWith(x => ApplyResult(x.Result));
		}
		return new NavigationResultResponse<TResult>(response?.Route ?? Route.Empty, ResultCompletion.Task, response?.Success ?? false, response?.Navigator ?? this);
	}

	public Task<bool> CanNavigate(Route route) => Navigation.CanNavigate(route);
}
