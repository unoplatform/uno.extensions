namespace Uno.Extensions;

public static class DispatcherExtensions
{
	public static async ValueTask ExecuteAsync(this IDispatcher dispatcher, AsyncAction action, CancellationToken token)
	{
		await dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			await action(t);
			return true;
		}, token);
	}

	public static async ValueTask ExecuteAsync(this IDispatcher dispatcher, Func<ValueTask> action)
	{
		await dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			await action();
			return true;
		}, CancellationToken.None);
	}

	public static ValueTask<TResult> ExecuteAsync<TResult>(this IDispatcher dispatcher, Func<ValueTask<TResult>> actionWithResult)
	{
		return dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			return await actionWithResult();
		}, CancellationToken.None);
	}

	public static async ValueTask ExecuteAsync(this IDispatcher dispatcher, Action action)
	{
		await dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			action();
			return true;
		}, CancellationToken.None);
	}

	public static async ValueTask ExecuteAsync(this IDispatcher dispatcher, Action<CancellationToken> action, CancellationToken token)
	{
		await dispatcher.ExecuteAsync(async (CancellationToken t) =>
		{
			action(t);
			return true;
		}, token);
	}
}
