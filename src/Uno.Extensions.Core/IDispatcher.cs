namespace Uno.Extensions;

public interface IDispatcher
{
	ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> actionWithResult, CancellationToken cancellation);
}
