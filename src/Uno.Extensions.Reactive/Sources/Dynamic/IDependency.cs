using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A dependency of a <see cref="DynamicFeed{T}"/> that is able to request a feed execution
/// </summary>
internal interface IDependency
{
	/// <summary>
	/// Callback invoked when an execution is starting.
	/// </summary>
	/// <param name="execution">The execution that is starting.</param>
	/// <param name="ct">A cancellation token to cancel this async action of the execution is being aborted.</param>
	/// <returns>An async operation.</returns>
	ValueTask OnExecuting(FeedExecution execution, CancellationToken ct);

	/// <summary>
	/// Callback invoked when an execution has completed.
	/// </summary>
	/// <param name="execution">The execution that has ended.</param>
	/// <param name="result">The result of the execution.</param>
	/// <param name="ct">A cancellation token to cancel this async action of the execution is being aborted.</param>
	/// <returns>An async operation.</returns>
	ValueTask OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct);
}
