namespace Uno.Extensions;

public interface IDispatcher
{
	Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> actionWithResult, CancellationToken cancellation);
}
