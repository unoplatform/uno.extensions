namespace Uno.Extensions;

public interface IDispatcher
{
	Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> actionWithResult);
}
