using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// Configurations for <see cref="FeedRecorder{TFeed,TValue}"/>
/// </summary>
public class FeedRecorder
{
	/// <summary>
	/// Gets the default timeout, in ms, used for <see cref="IFeedRecorder{T}.WaitForMessages"/> and <see cref="IFeedRecorder{T}.WaitForEnd"/>.
	/// </summary>
	/// <remarks>
	/// A passing test waits only as long as its feed actually takes, so this bound costs nothing
	/// when things work - it only decides how much scheduling delay counts as a failure. At 1s a
	/// contended CI agent produced spurious timeouts (<c>Given_PaginatedListFeed</c>
	/// <c>When_Async_Then_FlagIsLoading</c> reported 2 of 3 expected messages), so genuine failures
	/// were indistinguishable from a busy machine.
	/// </remarks>
	public const int DefaultTimeout = 5000;
}
