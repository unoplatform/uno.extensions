namespace Uno.Extensions;

public static class DispatcherQueueExtensions
{
	public static async Task<TResult> ExecuteAsync<TResult>(
		this DispatcherQueue dispatcher,
		Func<CancellationToken, Task<TResult>> actionWithResult,
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
							}
						});
		return await completion.Task;
	}

	public static Task<TResult> ExecuteAsync<TResult>(this DispatcherQueue dispatcher, Func<Task<TResult>> actionWithResult)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			return await actionWithResult();
		}, CancellationToken.None);
	}
}
