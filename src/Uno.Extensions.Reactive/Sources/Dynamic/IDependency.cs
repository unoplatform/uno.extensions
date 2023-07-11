using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

internal interface IDependency
{
	ValueTask OnLoading(FeedExecution execution, CancellationToken ct);

	ValueTask OnLoaded(FeedExecution execution, FeedAsyncExecutionResult result, CancellationToken ct);
}
