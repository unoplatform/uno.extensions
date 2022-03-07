﻿namespace Uno.Extensions.Navigation;

public class ResponseNavigator<TResult> : IResponseNavigator, IInstance<IServiceProvider>
{
	private INavigator Navigation { get; }

	private TaskCompletionSource<Option<TResult>> ResultCompletion { get; }

	public Route? Route => Navigation.Route;

	private DispatcherQueue Dispatcher { get; } = DispatcherQueue.GetForCurrentThread();

	public IServiceProvider? Instance => Navigation.Get<IServiceProvider>();

	public ResponseNavigator(INavigator internalNavigation, NavigationRequest request)
	{
		Navigation = internalNavigation;
		ResultCompletion = new TaskCompletionSource<Option<TResult>>();


		if (request.Cancellation.HasValue)
		{
			request.Cancellation.Value.Register(() =>
			{
				ApplyResult(Option.None<TResult>());
			});
		}


		// Replace the navigator
		Navigation.Get<IServiceProvider>()?.AddInstance<INavigator>(this);
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
			ApplyResult(result);
		}

		return navResponse;
	}

	private void ApplyResult(Option<TResult> responseData)
	{
		if (ResultCompletion.Task.Status == TaskStatus.Canceled ||
			ResultCompletion.Task.Status == TaskStatus.RanToCompletion)
		{
			return;
		}

		// Restore the navigator
		Navigation.Get<IServiceProvider>()?.AddInstance<INavigator>(this.Navigation);

		Dispatcher.TryEnqueue(() =>
		{
			ResultCompletion.TrySetResult(responseData);
		});
	}

	public NavigationResponse AsResponseWithResult(NavigationResponse? response)
	{
		if(response is NavigationResultResponse<TResult> navResponse)
		{
			navResponse.Result.ContinueWith(x => ApplyResult(x.Result));
		}
		return new NavigationResultResponse<TResult>(response?.Route ?? Route.Empty, ResultCompletion.Task, response?.Success ?? false);
	}
}
