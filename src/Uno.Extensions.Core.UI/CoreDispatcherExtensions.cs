namespace Uno.Extensions;

#if !WINUI
public static class CoreDispatcherExtensions
{
	public static async ValueTask<TResult> ExecuteAsync<TResult>(
		this Windows.UI.Core.CoreDispatcher dispatcher,
		AsyncFunc<TResult> actionWithResult,
		CancellationToken cancellation)
	{
		var completion = new TaskCompletionSource<TResult>();
		await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
		{
			try
			{
				var result = await actionWithResult(cancellation);
				completion.TrySetResult(result);
			}
			catch (Exception ex)
			{
				completion.TrySetException(ex);
				throw;
			}
		});
		return await completion.Task;
	}
}
#endif
