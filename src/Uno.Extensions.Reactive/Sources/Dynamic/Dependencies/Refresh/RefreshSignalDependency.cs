using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class RefreshSignalDependency : IDependency
{
	public RefreshSignalDependency(FeedSession session, ISignal signal)
	{
		signal
			.GetSource(session.Context)
			.ForEachAsync((_, i) => session.Execute(new ExecuteRequest(this, $"external refresh signal '{signal}' as been raised (count: {i})")), session.Token)
			.ContinueWith(_ => session.UnRegisterDependency(this), TaskContinuationOptions.ExecuteSynchronously);
	}

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuting(FeedExecution execution, CancellationToken ct) { }

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct) { }
}
