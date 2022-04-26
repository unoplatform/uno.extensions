namespace Uno.Extensions;

public static class DispatcherExtensions
{
	public static Task ExecuteAsync(this IDispatcher dispatcher, Func<CancellationToken, Task> action, CancellationToken token)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			await action(t);
			return true;
		}, token);
	}

	public static Task ExecuteAsync(this IDispatcher dispatcher, Func<Task> action)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			await action();
			return true;
		}, CancellationToken.None);
	}

	public static Task<TResult> ExecuteAsync<TResult>(this IDispatcher dispatcher, Func<Task<TResult>> actionWithResult)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			return await actionWithResult();
		}, CancellationToken.None);
	}

	public static Task ExecuteAsync(this IDispatcher dispatcher, Action action)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			action();
			return true;
		}, CancellationToken.None);
	}

	public static Task ExecuteAsync(this IDispatcher dispatcher, Action<CancellationToken> action, CancellationToken token)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			action(t);
			return true;
		}, token);
	}
}
