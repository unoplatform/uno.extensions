using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// The result of an execution of a <see cref="DynamicFeed{T}"/>.
/// </summary>
internal enum FeedExecutionResult
{
	/// <summary>
	/// The data has been loaded successfully.
	/// </summary>
	Success,

	/// <summary>
	/// The execution has failed.
	/// </summary>
	Failed,

	/// <summary>
	/// The execution has been cancelled (either because a new execution has been started, or because the session has been disposed).
	/// </summary>
	Cancelled,
}
