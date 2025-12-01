namespace Uno.Extensions.Navigation;

public class ResponseNavigator<TResult> : IResponseNavigator, IInstance<IServiceProvider>
{
	private INavigator Navigation { get; }

	private TaskCompletionSource<Option<TResult>> ResultCompletion { get; }

	public Route? Route => Navigation.Route;

	private IDispatcher Dispatcher => (Navigation as Navigator)!.Dispatcher;

	public IServiceProvider? Instance => Navigation.Get<IServiceProvider>();

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
			await CompleteWithResult(responseData);
		}

		return navResponse;
	}

	/// <inheritdoc />
	public async Task CompleteWithResult(object? responseData)
	{
		// Early return if already completed to avoid unnecessary processing
		if (ResultCompletion.Task.Status == TaskStatus.Canceled ||
			ResultCompletion.Task.Status == TaskStatus.RanToCompletion)
		{
			return;
		}

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

	private async Task ApplyResult(Option<TResult> responseData)
	{
		if (ResultCompletion.Task.Status == TaskStatus.Canceled ||
			ResultCompletion.Task.Status == TaskStatus.RanToCompletion)
		{
			return;
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
