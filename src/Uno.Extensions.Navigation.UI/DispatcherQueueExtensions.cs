namespace Uno.Extensions;

public static class DispatcherQueueExtensions
{
	public static Task Run(this DispatcherQueue dispatcher, Func<Task> action)
	{
		return dispatcher.Run(async () =>
		{
			await action();
			return true;
		});
	}

	public static async Task<TResult> Run<TResult>(this DispatcherQueue dispatcher, Func<Task<TResult>> actionWithResult)
	{
		var completion = new TaskCompletionSource<TResult>();
		dispatcher.TryEnqueue(async () =>
						{
							try
							{
								var result = await actionWithResult();
								completion.SetResult(result);
							}
							catch (Exception ex)
							{
								completion.SetException(ex);
							}
						});
		return await completion.Task;
	}
}
