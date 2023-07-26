using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Sources;

internal interface IDependency
{
	ValueTask OnExecuting(FeedExecution execution, CancellationToken ct);

	ValueTask OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct);
}
