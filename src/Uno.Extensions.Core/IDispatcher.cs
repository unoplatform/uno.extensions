namespace Uno.Extensions;

public interface IDispatcher
{
	/// <summary>
	/// Asynchronously executes an operation on the UI thread.
	/// </summary>
	/// <typeparam name="TResult">Type of the result of the operation.</typeparam>
	/// <param name="func">The async operation to execute.</param>
	/// <param name="cancellation">An cancellation token to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the result of the operation.</returns>
	ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> func, CancellationToken cancellation);

	/// <summary>
	///  Gets a value that specifies whether the current execution context is on the UI thread.
	/// </summary>
	bool HasThreadAccess { get; }
}
