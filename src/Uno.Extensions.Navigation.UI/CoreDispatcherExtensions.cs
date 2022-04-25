namespace Uno.Extensions;

#if !WINUI
public static class CoreDispatcherExtensions
{
	public static async Task<TResult> ExecuteAsync<TResult>(this Windows.UI.Core.CoreDispatcher dispatcher, Func<Task<TResult>> actionWithResult)
	{
		var completion = new TaskCompletionSource<TResult>();
		await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
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
#endif
