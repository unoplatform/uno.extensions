using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal enum ConcurrencyMode
{
	/// <summary>
	/// Queue async operation so there are run sequentially.
	/// </summary>
	Queue,

	/// <summary>
	/// Abort any pending async operation before starting a new one.
	/// </summary>
	AbortPrevious,

	/// <summary>
	/// Ignore any request to start a new async operation if a previous one is still running.
	/// </summary>
	IgnoreNew,

	/// <summary>
	/// Runs async operation in parallel.
	/// </summary>
	Parallel
}
