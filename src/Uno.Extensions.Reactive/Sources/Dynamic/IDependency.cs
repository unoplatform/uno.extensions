using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

internal interface IDependency
{
	ValueTask OnExecuting(FeedExecution execution, CancellationToken ct);

	ValueTask OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct);
}
