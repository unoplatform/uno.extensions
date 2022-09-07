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
	public const int DefaultTimeout = 1000;
}
