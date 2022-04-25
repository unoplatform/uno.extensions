namespace Uno.Extensions;

public static class DispatcherExtensions
{
	public static Task ExecuteAsync(this IDispatcher dispatcher, Func<Task> action)
	{
		return dispatcher.ExecuteAsync(async () =>
		{
			await action();
			return true;
		});
	}

	public static Task ExecuteAsync(this IDispatcher dispatcher, Action action)
	{
		return dispatcher.ExecuteAsync(async () =>
		{
			action();
			return true;
		});
	}
}
