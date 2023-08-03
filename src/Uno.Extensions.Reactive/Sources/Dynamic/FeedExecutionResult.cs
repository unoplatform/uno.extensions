using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

internal enum FeedExecutionResult
{
	Success,
	Failed,
	Cancelled,
}
