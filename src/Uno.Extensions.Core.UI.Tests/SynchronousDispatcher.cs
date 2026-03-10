namespace Uno.Extensions.Core.UI.Tests;

internal class SynchronousDispatcher : IDispatcher
{
	public bool HasThreadAccess => true;

	public bool TryEnqueue(Action action)
	{
		action();
		return true;
	}

	public ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> action, CancellationToken cancellation)
		=> action(cancellation);
}
