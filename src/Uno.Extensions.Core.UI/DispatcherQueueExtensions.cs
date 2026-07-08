namespace Uno.Extensions;

public static class DispatcherQueueExtensions
{
	public static async ValueTask<TResult> ExecuteAsync<TResult>(
		this DispatcherQueue dispatcher,
		AsyncFunc<TResult> actionWithResult,
		CancellationToken cancellation)
	{
		var completion = new TaskCompletionSource<TResult>();
		dispatcher.TryEnqueue(async () =>
						{
							try
							{
								var result = await actionWithResult(cancellation);
								completion.SetResult(result);
							}
							catch (Exception ex)
							{
								completion.SetException(ex);
								throw;
							}
						});
		return await completion.Task;
	}

	public static ValueTask<TResult> ExecuteAsync<TResult>(this DispatcherQueue dispatcher, Func<ValueTask<TResult>> actionWithResult)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			return await actionWithResult();
		}, CancellationToken.None);
	}
}
